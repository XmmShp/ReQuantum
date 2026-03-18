using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Abstraction;
using ReQuantum.Application.Models.Zdbk;
using ReQuantum.Application.Parsers;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;
using System.Net;
using System.Net.Http.Json;

namespace ReQuantum.Application.Services.Zdbk;

[AutoInject(Lifetime.Singleton)]
public class ZdbkExamService : IZdbkExamService
{
    private readonly IZjuContext _zjuContext;
    private readonly IHttpContext _httpContext;
    private readonly IAcademicCalendarService _calendarService;
    private readonly ILogger<ZdbkExamService> _logger;

    private const string ExamApiBase = "https://zdbk.zju.edu.cn/jwglxt/xskscx/kscx_cxXsgrksIndex.html";
    private const string SsoLoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string BaseUrl = "https://zdbk.zju.edu.cn";
    private const string SsoRedirectUrl = "/jwglxt/xtgl/login_ssologin.html";

    public ZdbkExamService(
        IZjuContext zjuContext,
        IHttpContext httpContext,
        IAcademicCalendarService calendarService,
        ILogger<ZdbkExamService> logger)
    {
        _zjuContext = zjuContext;
        _httpContext = httpContext;
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task<Result<List<ParsedExamInfo>>> GetExamsAsync()
    {
        if (!_zjuContext.IsAuthenticated || _zjuContext.LoginInfo is null)
        {
            return Result.Fail("400", "未登录或无学号");
        }

        var clientResult = await GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail("400", clientResult.Message);
        }

        try
        {
            var client = clientResult.Value!;
            var apiUrl = $"{ExamApiBase}?doType=query&gnmkdm=N509070&su={_zjuContext.LoginInfo.LoginName}";

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
                return Result.Fail("500", $"获取考试信息失败: {response.StatusCode}");
            }

            var examResponse = await response.Content.ReadFromJsonAsync<ZdbkExamResponse>();
            if (examResponse == null)
            {
                return Result.Fail("500", "解析考试数据失败");
            }

            var calendarResult = await _calendarService.GetCurrentCalendarAsync();
            var calendar = calendarResult.IsSuccess ? calendarResult.Value : null;

            var exams = ParseExams(examResponse.Items, calendar);
            _logger.LogInformation("成功获取 {Count} 条考试信息", exams.Count);
            return exams;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取考试信息时发生错误");
            return Result.Fail("500", $"获取考试信息失败: {ex.Message}");
        }
    }

    private static List<ParsedExamInfo> ParseExams(List<ZdbkExamDto> rawExams, AcademicCalendar? calendar)
    {
        var result = new List<ParsedExamInfo>();
        foreach (var raw in rawExams)
        {
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

    private async Task<Result<HttpClient>> GetAuthenticatedClientAsync()
    {
        try
        {
            var cookies = new List<Cookie>();
            var ssoUrl = $"{SsoLoginUrl}?service={Uri.EscapeDataString($"{BaseUrl}{SsoRedirectUrl}")}";
            using var response = await HttpClientUtilities.GetWithCookieTrackingAsync(_httpContext.HttpClient, ssoUrl, cookies);

            var sessionCookie = cookies.Last(ck => ck is { Name: "JSESSIONID", Domain: "zdbk.zju.edu.cn" });
            var route = cookies.Last(ck => ck is { Name: "route" });

            return HttpClientUtilities.Create(new RequestOptions { Cookies = [sessionCookie, route] });
        }
        catch (Exception ex)
        {
            return Result.Fail("500", $"SSO认证失败: {ex.Message}");
        }
    }
}
