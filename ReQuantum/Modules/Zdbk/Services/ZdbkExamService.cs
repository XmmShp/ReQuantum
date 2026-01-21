using Microsoft.Extensions.Logging;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Modules.Zdbk.Parsers;
using ReQuantum.Modules.ZjuSso.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkExamService
{
    /// <summary>
    /// 获取考试信息
    /// </summary>
    Task<Result<List<ParsedExamInfo>>> GetExamsAsync();
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkExamService : IZdbkExamService
{
    private readonly IZjuSsoService _zjuSsoService;
    private readonly IAcademicCalendarService _calendarService;
    private readonly ILogger<ZdbkExamService> _logger;
    private readonly IStorage _storage;
    private readonly ILocalizer _localizer;

    private const string ExamApiBase = "https://zdbk.zju.edu.cn/jwglxt/xskscx/kscx_cxXsgrksIndex.html";
    private const string SsoLoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string BaseUrl = "https://zdbk.zju.edu.cn";
    private const string SsoRedirectUrl = "/jwglxt/xtgl/login_ssologin.html";

    public ZdbkExamService(
        IZjuSsoService zjuSsoService,
        IAcademicCalendarService calendarService,
        IStorage storage,
        ILogger<ZdbkExamService> logger,
        ILocalizer localizer)
    {
        _zjuSsoService = zjuSsoService;
        _calendarService = calendarService;
        _storage = storage;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<Result<List<ParsedExamInfo>>> GetExamsAsync()
    {
        if (!_zjuSsoService.IsAuthenticated || string.IsNullOrEmpty(_zjuSsoService.Id))
        {
            return Result.Fail(_localizer[nameof(UIText.NotLoggedInOrNoStudentId)]);
        }

        // 获取已认证的客户端
        var clientResult = await GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }

        try
        {
            var client = clientResult.Value;
            var apiUrl = $"{ExamApiBase}?doType=query&gnmkdm=N509070&su={_zjuSsoService.Id}";

            var formData = new Dictionary<string, string>
              {
                  { "_search", "false" },
                  { "nd", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString() },
                  { "queryModel.showCount", "5000" },
                  { "queryModel.currentPage", "1" },
                  { "queryModel.sortName", "xkkh" },
                  { "queryModel.sortOrder", "asc" },
                  { "time", "0" }
              };

            var content = new FormUrlEncodedContent(formData);
            var response = await client.PostAsync(apiUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail($"{_localizer[nameof(UIText.GetExamInfoFailed)]}: {response.StatusCode}");
            }

            var examResponse = await response.Content.ReadFromJsonAsync(
                SourceGenerationContext.Default.ZdbkExamResponse);

            if (examResponse == null)
            {
                return Result.Fail(_localizer[nameof(UIText.ParseExamDataFailed)]);
            }

            // 获取校历用于时间解析
            var calendarResult = await _calendarService.GetCurrentCalendarAsync();
            var calendar = calendarResult.IsSuccess ? calendarResult.Value : null;

            // 解析考试信息
            var exams = ParseExams(examResponse.Items, calendar);

            _logger.LogInformation("成功获取 {Count} 条考试信息", exams.Count);
            return Result.Success(exams);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试信息时发生错误");
            return Result.Fail($"{_localizer[nameof(UIText.GetExamInfoFailed)]}: {ex.Message}");
        }
    }

    private List<ParsedExamInfo> ParseExams(List<ZdbkExamDto> rawExams, AcademicCalendar? calendar)
    {
        var result = new List<ParsedExamInfo>();

        foreach (var raw in rawExams)
        {
            // 期中考试
            if (!string.IsNullOrWhiteSpace(raw.MidTermExamTime))
            {
                var (start, end) = ExamTimeParser.Parse(raw.MidTermExamTime, calendar);
                result.Add(new ParsedExamInfo
                {
                    ClassId = raw.CourseId.Length >= 22 ? raw.CourseId[..22] : raw.CourseId,
                    CourseName = raw.CourseName.Replace("(", "（").Replace(")", "）"),
                    Credit = float.TryParse(raw.Credit, out var credit) ? credit : 0f,
                    ExamType = ExamType.MidTerm,
                    StartTime = start,
                    EndTime = end,
                    Location = raw.MidTermExamLocation,
                    Seat = raw.MidTermExamSeat,
                    RawTimeString = raw.MidTermExamTime
                });
            }

            // 期末考试
            if (!string.IsNullOrWhiteSpace(raw.FinalExamTime))
            {
                var (start, end) = ExamTimeParser.Parse(raw.FinalExamTime, calendar);
                result.Add(new ParsedExamInfo
                {
                    ClassId = raw.CourseId.Length >= 22 ? raw.CourseId[..22] : raw.CourseId,
                    CourseName = raw.CourseName.Replace("(", "（").Replace(")", "）"),
                    Credit = float.TryParse(raw.Credit, out var credit) ? credit : 0f,
                    ExamType = ExamType.FinalTerm,
                    StartTime = start,
                    EndTime = end,
                    Location = raw.FinalExamLocation,
                    Seat = raw.FinalExamSeat,
                    RawTimeString = raw.FinalExamTime
                });
            }

            // 无考试
            if (string.IsNullOrWhiteSpace(raw.MidTermExamTime) && string.IsNullOrWhiteSpace(raw.FinalExamTime))
            {
                result.Add(new ParsedExamInfo
                {
                    ClassId = raw.CourseId.Length >= 22 ? raw.CourseId[..22] : raw.CourseId,
                    CourseName = raw.CourseName.Replace("(", "（").Replace(")", "）"),
                    Credit = float.TryParse(raw.Credit, out var credit) ? credit : 0f,
                    ExamType = ExamType.NoExam
                });
            }
        }

        return result;
    }

    private async Task<Result<RequestClient>> GetAuthenticatedClientAsync()
    {
        var clientResult = await _zjuSsoService.GetAuthenticatedClientAsync(
            new RequestOptions { AllowRedirects = true });

        if (!clientResult.IsSuccess) return Result.Fail(clientResult.Message);

        try
        {
            var client = clientResult.Value;
            var ssoUrl = $"{SsoLoginUrl}?service={Uri.EscapeDataString($"{BaseUrl}{SsoRedirectUrl}")}";
            await client.GetAsync(ssoUrl);

            var allCookies = client.CookieContainer.GetAllCookies();
            var sessionCookie = allCookies.Last(ck => ck is { Name: "JSESSIONID", Domain: "zdbk.zju.edu.cn" });
            var route = allCookies.Last(ck => ck is { Name: "route" });

            return Result.Success(RequestClient.Create(new RequestOptions { Cookies = [sessionCookie, route] }));
        }
        catch (Exception ex)
        {
            return Result.Fail($"{_localizer[nameof(UIText.SsoAuthFailed)]}: {ex.Message}");
        }
    }
}
