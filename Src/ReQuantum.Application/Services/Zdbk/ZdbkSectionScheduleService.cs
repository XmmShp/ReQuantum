using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Models.Zdbk;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;
using System.Net.Http.Json;

namespace ReQuantum.Application.Services.Zdbk;

[AutoInject(Lifetime.Singleton)]
public class ZdbkSectionScheduleService : IZdbkSectionScheduleService
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
        try
        {
            var calendarResult = await _calendarService.GetCurrentCalendarAsync();
            if (!calendarResult.IsSuccess)
            {
                return Result.Fail("500", $"无法获取校历: {calendarResult.Message}");
            }

            var calendar = calendarResult.Value!;
            var currentDate = DateOnly.FromDateTime(DateTime.Now);
            var weekNumber = calendar.GetWeekNumber(currentDate);

            if (weekNumber == null)
            {
                return Result.Fail("400", "当前日期不在学期内");
            }

            var currentSemester = calendar.GetSemesterNameForWeek(weekNumber.Value);
            var currentYear = calendar.AcademicYear;
            var (semester1, semester2) = GetRelatedSemesters(currentSemester);

            var task1 = GetCourseScheduleAsync(currentYear, semester1);
            var task2 = GetCourseScheduleAsync(currentYear, semester2);
            await Task.WhenAll(task1, task2);

            var result1 = await task1;
            var result2 = await task2;
            var combinedSections = new List<ZdbkSectionDto>();

            if (result1.IsSuccess)
            {
                combinedSections.AddRange(result1.Value!.SectionList);
            }

            if (result2.IsSuccess)
            {
                combinedSections.AddRange(result2.Value!.SectionList);
            }

            if (combinedSections.Count == 0)
            {
                return Result.Fail("500", "所有学期获取失败");
            }

            return new ZdbkSectionScheduleResponse
            {
                SectionList = combinedSections,
                AcademicYear = currentYear,
                Semester = currentSemester,
                RelatedSemesters = [semester1, semester2],
                StudentId = result1.IsSuccess ? result1.Value!.StudentId : result2.Value?.StudentId,
                StudentName = result1.IsSuccess ? result1.Value!.StudentName : result2.Value?.StudentName,
                AdministrativeClass = result1.IsSuccess ? result1.Value!.AdministrativeClass : result2.Value?.AdministrativeClass,
                College = result1.IsSuccess ? result1.Value!.College : result2.Value?.College,
                Major = result1.IsSuccess ? result1.Value!.Major : result2.Value?.Major
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schedules");
            return Result.Fail("500", $"获取课程失败: {ex.Message}");
        }
    }

    private static (string, string) GetRelatedSemesters(string currentSemester) => currentSemester switch
    {
        "秋" or "冬" => ("秋", "冬"),
        "春" or "夏" => ("春", "夏"),
        _ => ("秋", "冬")
    };

    public async Task<Result<ZdbkSectionScheduleResponse>> GetCourseScheduleAsync(string academicYear, string semester)
    {
        var clientResult = await GetAuthenticatedClient();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail("400", clientResult.Message);
        }

        var client = clientResult.Value!;
        if (!_zjuSsoService.IsAuthenticated || string.IsNullOrEmpty(_zjuSsoService.Id))
        {
            return Result.Fail("400", "未找到学号");
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
            var response = await client.PostAsync(apiUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                _state = null;
                return Result.Fail("400", $"获取课程失败: {response.StatusCode}");
            }

            var scheduleResponse = await response.Content.ReadFromJsonAsync<ZdbkSectionScheduleResponse>();
            if (scheduleResponse is null)
            {
                return Result.Fail("400", "解析课程数据失败");
            }

            return scheduleResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting schedule");
            return Result.Fail("400", $"获取课程失败: {ex.Message}");
        }
    }

    private async Task<Result<RequestClient>> GetAuthenticatedClient()
    {
        var clientResult = await _zjuSsoService.GetAuthenticatedClientAsync(new RequestOptions { AllowRedirects = true });
        if (!clientResult.IsSuccess)
        {
            return Result.Fail("500", clientResult.Message);
        }

        try
        {
            var client = clientResult.Value!;
            var ssoUrl = $"{SsoLoginUrl}?service={Uri.EscapeDataString($"{BaseUrl}{SsoRedirectUrl}")}";
            await client.GetAsync(ssoUrl);

            var allCookies = client.CookieContainer.GetAllCookies();
            var sessionCookie = allCookies.Last(ck => ck is { Name: "JSESSIONID", Domain: "zdbk.zju.edu.cn" });
            var route = allCookies.Last(ck => ck is { Name: "route" });

            _state = new ZdbkState(sessionCookie, route);
            SaveState();
            return RequestClient.Create(new RequestOptions { Cookies = [sessionCookie, route] });
        }
        catch (Exception ex)
        {
            return Result.Fail("500", $"SSO认证失败: {ex.Message}");
        }
    }

    private static string MapSemesterToCode(string semester) => semester switch
    {
        "秋" or "冬" => "1",
        "春" or "夏" => "2",
        _ => throw new ArgumentOutOfRangeException(nameof(semester))
    };

    private void LoadState() => _state = (_storage.TryGetAsync<ZdbkState>(StateKey).AsTask()).GetAwaiter().GetResult().ValueOr((ZdbkState?)null);

    private void SaveState()
    {
        if (_state is null)
        {
            _storage.RemoveAsync(StateKey);
        }
        else
        {
            _storage.SetAsync(StateKey, _state);
        }
    }
}
