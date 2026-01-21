using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Modules.ZjuSso.Services;
using ReQuantum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkSectionScheduleService
{
    Task<Result<ZdbkSectionScheduleResponse>> GetCourseScheduleAsync(string academicYear, string semester);
    Task<Result<ZdbkSectionScheduleResponse>> GetCurrentSemesterScheduleAsync();
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkSectionScheduleService : IZdbkSectionScheduleService, IDaemonService
{
    private readonly IZjuSsoService _zjuSsoService;
    private readonly IAcademicCalendarService _calendarService;
    private readonly IStorage _storage;
    private readonly ILogger<ZdbkSectionScheduleService> _logger;
    private ZdbkState? _state;

    private const string StateKey = "Zdbk:State";
    private const string BaseUrl = "https://zdbk.zju.edu.cn";
    private const string SsoLoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string SsoRedirectUrl = "/jwglxt/xtgl/login_ssologin.html";
    private const string CourseScheduleApiBase = "https://zdbk.zju.edu.cn/jwglxt/kbcx/xskbcx_cxXsKb.html";

    public ZdbkSectionScheduleService(
        IZjuSsoService zjuSsoService,
        IAcademicCalendarService calendarService,
        IStorage storage,
        ILogger<ZdbkSectionScheduleService> logger)
    {
        _zjuSsoService = zjuSsoService;
        _calendarService = calendarService;
        _storage = storage;
        _logger = logger;
        _zjuSsoService.OnLogout += () => _state = null;
        LoadState();
    }

    public async Task<Result<ZdbkSectionScheduleResponse>> GetCurrentSemesterScheduleAsync()
    {
        _logger.LogInformation("Fetching current semester and related sub-semesters");

        try
        {
            var calendarResult = await _calendarService.GetCurrentCalendarAsync();
            if (!calendarResult.IsSuccess)
            {
                return Result.Fail($"无法获取校历：{calendarResult.Message}");
            }

            var calendar = calendarResult.Value;
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            var weekNumber = calendar.GetWeekNumber(currentDate);

            if (weekNumber == null)
            {
                return Result.Fail("当前日期不在学期范围内");
            }

            var currentSemester = calendar.GetSemesterNameForWeek(weekNumber.Value);
            var currentYear = calendar.AcademicYear;
            var (semester1, semester2) = GetRelatedSemesters(currentSemester);

            _logger.LogInformation("Fetching {Year} {S1} and {S2}", currentYear, semester1, semester2);

            // 并行获取两个学期的课程
            var task1 = GetCourseScheduleAsync(currentYear, semester1);
            var task2 = GetCourseScheduleAsync(currentYear, semester2);
            await Task.WhenAll(task1, task2);

            var result1 = await task1;
            var result2 = await task2;
            var combinedSections = new List<ZdbkSectionDto>();

            if (result1.IsSuccess) combinedSections.AddRange(result1.Value.SectionList);
            if (result2.IsSuccess) combinedSections.AddRange(result2.Value.SectionList);

            if (combinedSections.Count == 0)
            {
                return Result.Fail("所有学期均无法获取");
            }

            // 返回合并的结果，但保留两个学期的信息
            return Result.Success(new ZdbkSectionScheduleResponse
            {
                SectionList = combinedSections,
                AcademicYear = currentYear,
                Semester = currentSemester, // 当前学期
                RelatedSemesters = new[] { semester1, semester2 }, // 新增：保存两个学期名称
                StudentId = result1.IsSuccess ? result1.Value.StudentId : result2.Value?.StudentId,
                StudentName = result1.IsSuccess ? result1.Value.StudentName : result2.Value?.StudentName,
                AdministrativeClass = result1.IsSuccess ? result1.Value.AdministrativeClass : result2.Value?.AdministrativeClass,
                College = result1.IsSuccess ? result1.Value.College : result2.Value?.College,
                Major = result1.IsSuccess ? result1.Value.Major : result2.Value?.Major
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schedules");
            return Result.Fail($"获取课程表失败：{ex.Message}");
        }
    }

    private static (string, string) GetRelatedSemesters(string currentSemester)
    {
        return currentSemester switch
        {
            "秋" or "冬" => ("秋", "冬"),
            "春" or "夏" => ("春", "夏"),
            _ => ("秋", "冬")
        };
    }

    public async Task<Result<ZdbkSectionScheduleResponse>> GetCourseScheduleAsync(string academicYear, string semester)
    {
        var clientResult = await GetAuthenticatedClient();
        if (!clientResult.IsSuccess) return Result.Fail(clientResult.Message);

        var client = clientResult.Value;
        if (!_zjuSsoService.IsAuthenticated || string.IsNullOrEmpty(_zjuSsoService.Id))
        {
            return Result.Fail("未获取到学号信息");
        }

        try
        {
            var semesterCode = MapSemesterToCode(semester);
            var apiUrl = $"{CourseScheduleApiBase}?gnmkdm=N253508&su={_zjuSsoService.Id}";
            var formData = new Dictionary<string, string>
            {
                { "xnm", academicYear },
                { "xqm", $"{semesterCode}|{semester}" }
            };

            var content = new FormUrlEncodedContent(formData);
            content.Headers.ContentType?.CharSet = "utf-8";

            _logger.LogInformation("Requesting {Semester} courses with xqm={XqmValue}", semester, $"{semesterCode}|{semester}");

            var response = await client.PostAsync(apiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                _state = null;
                return Result.Fail($"获取课程表失败: {response.StatusCode}");
            }

            var scheduleResponse = await response.Content.ReadFromJsonAsync(
                SourceGenerationContext.Default.ZdbkSectionScheduleResponse);

            if (scheduleResponse is null)
            {
                return Result.Fail("解析课程表数据失败");
            }

            // 添加日志：显示返回的课程的Term字段
            _logger.LogInformation("Received {Count} courses for {Semester}, first course Term: {FirstTerm}",
                scheduleResponse.SectionList.Count,
                semester,
                scheduleResponse.SectionList.FirstOrDefault()?.Term ?? "N/A");

            return Result.Success(scheduleResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule");
            return Result.Fail($"获取课程表失败：{ex.Message}");
        }
    }

    private async Task<Result<RequestClient>> GetAuthenticatedClient()
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

            _state = new ZdbkState(sessionCookie, route);
            SaveState();

            return Result.Success(RequestClient.Create(new RequestOptions { Cookies = [sessionCookie, route] }));
        }
        catch (Exception ex)
        {
            return Result.Fail($"SSO认证失败：{ex.Message}");
        }
    }

    private static string MapSemesterToCode(string semester) => semester switch
    {
        "秋" or "冬" => "1",
        "春" or "夏" => "2",
        _ => throw new ArgumentOutOfRangeException(nameof(semester))
    };

    private void LoadState() => _storage.TryGetWithEncryption(StateKey, out _state);

    private void SaveState()
    {
        if (_state is null) _storage.Remove(StateKey);
        else _storage.SetWithEncryption(StateKey, _state);
    }
}

public static class CalendarEventExtensions
{
    private const string Zdbk = "Zdbk";
    private const string ZdbkExam = "ZdbkExam";

    extension(CalendarEvent evt)
    {
        public bool IsFromZdbk
        {
            get => evt.From == Zdbk;
            set
            {
                if (evt.From == Zdbk && !value) evt.From = string.Empty;
                else if (evt.From != Zdbk && value) evt.From = Zdbk;
            }
        }

        public bool IsFromZdbkExam
        {
            get => evt.From == ZdbkExam;
            set
            {
                if (evt.From == ZdbkExam && !value) evt.From = string.Empty;
                else if (evt.From != ZdbkExam && value) evt.From = ZdbkExam;
            }
        }
    }
}
