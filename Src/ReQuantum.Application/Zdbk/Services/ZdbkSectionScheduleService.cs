using LoginZju;
using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Zdbk.Models;
using ReQuantum.Application.ZjuSso.Services;
using System.Net.Http.Json;

namespace ReQuantum.Application.Zdbk.Services;

[AutoInject(Lifetime.Singleton)]
public class ZdbkSectionScheduleService : IZdbkSectionScheduleService
{
    private readonly IZjuContext _zjuContext;
    private readonly ILoginZjuFactory _loginZjuFactory;
    private readonly ZjuamAuthHolder _authHolder;
    private readonly IAcademicCalendarService _calendarService;
    private readonly ILogger<ZdbkSectionScheduleService> _logger;
    private IZjuamAuth? _cachedAuth;
    private IZdbkService? _cachedZdbkService;

    private const string CourseScheduleApiBase = "https://zdbk.zju.edu.cn/jwglxt/kbcx/xskbcx_cxXsKb.html";

    public ZdbkSectionScheduleService(
        IZjuContext zjuContext,
        ILoginZjuFactory loginZjuFactory,
        ZjuamAuthHolder authHolder,
        IAcademicCalendarService calendarService,
        ILogger<ZdbkSectionScheduleService> logger)
    {
        _zjuContext = zjuContext;
        _loginZjuFactory = loginZjuFactory;
        _authHolder = authHolder;
        _calendarService = calendarService;
        _logger = logger;
        _zjuContext.OnLogout += () => ResetCachedService();
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
        var zdbkServiceResult = GetZdbkService();
        if (!zdbkServiceResult.IsSuccess)
        {
            return Result.Fail("400", zdbkServiceResult.Message);
        }

        var zdbkService = zdbkServiceResult.Value!;
        if (!_zjuContext.IsAuthenticated || _zjuContext.LoginInfo is null)
        {
            return Result.Fail("400", "未找到学号");
        }

        try
        {
            var semesterCode = MapSemesterToCode(semester);
            var apiUrl = $"{CourseScheduleApiBase}?gnmkdm=N253508&su={_zjuContext.LoginInfo.LoginName}";
            var formData = new Dictionary<string, string>
            {
                { "xnm", academicYear },
                { "xqm", $"{semesterCode}|{semester}" }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            using var response = await zdbkService.FetchAsync(request);
            if (!response.IsSuccessStatusCode)
            {
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

    private Result<IZdbkService> GetZdbkService()
    {
        var auth = _authHolder.CurrentAuth;
        if (auth is null)
        {
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

    private static string MapSemesterToCode(string semester) => semester switch
    {
        "秋" or "冬" => "1",
        "春" or "夏" => "2",
        _ => throw new ArgumentOutOfRangeException(nameof(semester))
    };

    private void ResetCachedService()
    {
        _cachedAuth = null;
        _cachedZdbkService?.Dispose();
        _cachedZdbkService = null;
    }
}
