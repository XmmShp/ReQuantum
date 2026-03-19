using LoginZju;
using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Zdbk.Models;
using ReQuantum.Application.Zdbk.Utilities;
using ReQuantum.Application.ZjuSso.Services;
using System.Net.Http.Json;

namespace ReQuantum.Application.Zdbk.Services;

[AutoInject(Lifetime.Singleton)]
public class ZdbkExamService : IZdbkExamService
{
    private readonly ILoginZjuFactory _loginZjuFactory;
    private readonly ZjuAuthAccessor _authAccessor;
    private readonly IAcademicCalendarService _calendarService;
    private readonly ILogger<ZdbkExamService> _logger;
    private IZjuamAuth? _cachedAuth;
    private IZdbkService? _cachedZdbkService;

    private const string ExamApiBase = "https://zdbk.zju.edu.cn/jwglxt/xskscx/kscx_cxXsgrksIndex.html";

    public ZdbkExamService(
        ILoginZjuFactory loginZjuFactory,
        ZjuAuthAccessor authAccessor,
        IAcademicCalendarService calendarService,
        ILogger<ZdbkExamService> logger)
    {
        _loginZjuFactory = loginZjuFactory;
        _authAccessor = authAccessor;
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task<Result<List<ParsedExamInfo>>> GetExamsAsync()
    {
        var loginInfo = _authAccessor.CurrentLoginInfo;
        if (loginInfo is null)
        {
            return Result.Fail("400", "未登录或无学号");
        }

        var zdbkServiceResult = GetZdbkService();
        if (!zdbkServiceResult.IsSuccess)
        {
            return Result.Fail("400", zdbkServiceResult.Message);
        }

        try
        {
            var zdbkService = zdbkServiceResult.Value!;
            var apiUrl = $"{ExamApiBase}?doType=query&gnmkdm=N509070&su={loginInfo.LoginName}";

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

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            using var response = await zdbkService.FetchAsync(request);

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

    private Result<IZdbkService> GetZdbkService()
    {
        var auth = _authAccessor.Current;
        if (auth is null)
        {
            ResetCachedService();
            return Result.Fail("400", "未登录或登录状态已过期");
        }

        if (_cachedZdbkService is null || !ReferenceEquals(_cachedAuth, auth))
        {
            _cachedZdbkService?.Dispose();
            _cachedAuth = auth;
            _cachedZdbkService = _loginZjuFactory.CreateZdbk(auth);
        }

        return Result.Success(_cachedZdbkService!);
    }

    private void ResetCachedService()
    {
        _cachedAuth = null;
        _cachedZdbkService?.Dispose();
        _cachedZdbkService = null;
    }
}
