#pragma warning disable IL2026 // JSON serialization may require types that cannot be statically analyzed
#pragma warning disable IL3050 // JSON serialization may need runtime code generation for AOT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Models;


namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkGradeService
{
    /// <param name="academicYear">学年（如 "2024-2025"）</param>
    /// <param name="semester">学期（如 "秋"、"冬"、"春"、"夏"）</param>
    Task<Result<ZdbkGrades>> GetSemesterGradesAsync(string academicYear, string semester);


    Task<Result<ZdbkGrades>> GetGradesAsync();

    /// <summary>
    /// 刷新成绩
    /// </summary>
    Task<Result<ZdbkGrades>> RefreshGradesAsync();

    /// <summary>
    /// 获取缓存的成绩
    /// </summary>
    ZdbkGrades? GetCachedGrades();
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkGradeService : IZdbkGradeService, IDaemonService
{
    private readonly IStorage _storage;
    private readonly IZdbkSessionService _sessionService;
    private readonly ILogger<ZdbkGradeService> _logger;

    private const string StorageKey = "Zdbk:Grades";
    private const string BaseUrl = "https://zdbk.zju.edu.cn/jwglxt";
    private const string GetGradeUrl = "/cxdy/xscjcx_cxXscjIndex.html";
    private ZdbkGrades? _cachedGrades;

    public ZdbkGradeService(
        IStorage storage,
        IZdbkSessionService sessionService,
        ILogger<ZdbkGradeService> logger)
    {
        _storage = storage;
        _sessionService = sessionService;
        _logger = logger;

        LoadCachedGrades();
    }



    private void SaveGrades(ZdbkGrades grades)
    {
        _logger.LogInformation("保存成绩缓存");
        EnsureCoursesCollection(grades);
        _cachedGrades = grades;
        _storage.Set(StorageKey, grades);
        _logger.LogDebug("Saved grades to storage");
    }
    /// <summary>
    /// 检查响应是否为重定向（Session 失效）
    /// </summary>
    private bool IsSessionExpired(HttpResponseMessage response)
    {
        if (response.StatusCode == System.Net.HttpStatusCode.Found ||
            response.StatusCode == System.Net.HttpStatusCode.Redirect ||
            response.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
        {
            _logger.LogWarning("ZDBK Session 已失效");
            _sessionService.ClearState();
            return true;
        }
        return false;
    }

    public async Task<Result<ZdbkGrades>> GetGradesAsync()
    {
        if (_cachedGrades != null)
        {
            _logger.LogDebug("Using cached grades");
            return Result.Success(_cachedGrades);
        }

        var refreshResult = await RefreshGradesAsync();
        return refreshResult;
    }

    public async Task<Result<ZdbkGrades>> GetSemesterGradesAsync(string academicYear, string semester)
    {
        if (_cachedGrades != null)
        {
            _logger.LogDebug("使用成绩缓存数据 {Year} {Semester}", academicYear, semester);
            return Result.Success(FilterGrades(_cachedGrades, academicYear, semester));
        }

        _logger.LogDebug("缓存中暂无成绩数据");
        var refreshResult = await RefreshGradesAsync();
        if (!refreshResult.IsSuccess || refreshResult.Value == null)
        {
            return Result.Fail(refreshResult.Message);
        }

        _logger.LogDebug("成功获取成绩数据 {Year} {Semester}", academicYear, semester);
        return Result.Success(FilterGrades(refreshResult.Value, academicYear, semester));
    }

    public async Task<Result<ZdbkGrades>> RefreshGradesAsync()
    {
        _logger.LogDebug("更新成绩数据");
        var fetchResult = await FetchGradesAsync(null, null);
        if (!fetchResult.IsSuccess || fetchResult.Value == null)
        {
            return Result.Fail(fetchResult.Message);
        }

        SaveGrades(fetchResult.Value);
        return Result.Success(fetchResult.Value);
    }

    public ZdbkGrades? GetCachedGrades()
    {
        return _cachedGrades;
    }

    private void LoadCachedGrades()
    {
        if (_storage.TryGet(StorageKey, out ZdbkGrades? cached) && cached is not null)
        {
            _cachedGrades = cached;
            _logger.LogDebug("加载本地成绩缓存");
        }
    }

    private async Task<Result<ZdbkGrades>> FetchGradesAsync(string? academicYear, string? semester)
    {

        _logger.LogDebug("拉取成绩数据");
        // 获取认证客户端
        var clientResult = await _sessionService.GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }


        var state = _sessionService.State;
        if (state == null)
        {
            return Result.Fail("无法获取 ZDBK 会话状态");
        }


        using var client = clientResult.Value;

        try
        {
            var requestUrl = $"{BaseUrl}{GetGradeUrl}?doType=query&gnmkdm=N508301&su={state.StudentId}";

            var termCode = ConvertToTermCode(semester);

            // 构造 POST 报文
            var formData = new Dictionary<string, string>
            {
                { "xn", academicYear ?? string.Empty },
                { "xq", termCode ?? string.Empty},
                { "zscjl", "" },
                { "zscjr", "" },
                { "_search", "false" },
                { "queryModel.currentPage", "1" },
                { "queryModel.showCount", "5000" },
                { "queryModel.sortName", "xkkh"},
                { "queryModel.sortOrder", "asc" },
                { "time", "1"  }
            };

            var content = new FormUrlEncodedContent(formData);

            // 发送请求
            var response = await client.PostAsync(requestUrl, content);

            // 检查 Session 是否失效
            if (IsSessionExpired(response))
            {
                return Result.Fail("登录状态已失效，请重新登录后重试");
            }

            response.EnsureSuccessStatusCode();

            // 解析 JSON 数据
            var jsonContent = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;
            IEnumerable<JsonElement> items;

            if (root.ValueKind == JsonValueKind.Array)
            {
                items = root.EnumerateArray();
            }
            else if (root.ValueKind == JsonValueKind.Object &&
                     root.TryGetProperty("items", out var itemsProp) &&
                     itemsProp.ValueKind == JsonValueKind.Array)
            {
                items = itemsProp.EnumerateArray();
            }
            else
            {
                return Result.Fail("解析课程数据失败：响应格式不符合预期");
            }

            var grades = new ZdbkGrades
            {
                CoursesGrade = items.Select(json =>
                {
                    var xkkh = json.TryGetProperty("xkkh", out var xkkhProp) ? xkkhProp.GetString() ?? "" : "";
                    var match = Regex.Match(xkkh, @"^\((?<year>\d{4}-\d{4})-(?<term>\d+)\)-(?<code>[^-]+)");
                    string extractedYear = string.Empty;
                    string extractedTerm = string.Empty;
                    string courseCode = string.Empty;
                    if (match.Success)
                    {
                        extractedYear = match.Groups["year"].Value;
                        extractedTerm = match.Groups["term"].Value;
                        courseCode = match.Groups["code"].Value;
                    }
                    return new ZdbkCoursesGrade
                    {
                        CourseName = json.TryGetProperty("kcmc", out var kcmc) ? kcmc.GetString() ?? "" : "",
                        CourseCode = courseCode,
                        Grade100 = json.TryGetProperty("cj", out var cj) && double.TryParse(cj.GetString(), out var g100) ? g100 : 0,
                        Grade5 = json.TryGetProperty("jd", out var jd) && double.TryParse(jd.GetString(), out var g5) ? g5 : 0,
                        Credit = json.TryGetProperty("xf", out var xf) && double.TryParse(xf.GetString(), out var cr) ? cr : 0,
                        AcademicYear = extractedYear,
                        Semester = extractedTerm
                    };
                }).Where(c =>
                    (string.IsNullOrWhiteSpace(academicYear) || c.AcademicYear == academicYear) &&
                    (string.IsNullOrWhiteSpace(termCode) || c.Semester == termCode))
                .ToList()

            };

            // 汇总计算
            grades.Credit = grades.CoursesGrade.Sum(x => x.Credit);
            if (grades.Credit > 0)
            {
                grades.GradePoint5 = grades.CoursesGrade.Sum(x => x.Grade5 * x.Credit) / grades.Credit;
                grades.GradePoint100 = grades.CoursesGrade.Sum(x => x.Grade100 * x.Credit) / grades.Credit;
                grades.MajorCredit = grades.Credit;
                grades.MajorGradePoint = grades.GradePoint5;
                grades.GradePoint4 = grades.GradePoint5 * 4 / 5;

            }
            _logger.LogDebug($"成功拉取{grades.CoursesGrade.Count}门成绩数据");
            return Result.Success(grades);
        }
        catch (Exception ex)
        {
            return Result.Fail($"获取成绩失败: {ex.Message}");
        }

    }

    private static string? ConvertToTermCode(string? semester)
    {
        return semester switch
        {
            "秋" or "冬" or "秋冬" => "1",
            "春" or "夏" or "春夏" => "2",
            _ => null
        };
    }

    private static ZdbkGrades FilterGrades(ZdbkGrades source, string academicYear, string semester)
    {
        EnsureCoursesCollection(source);
        var termCode = ConvertToTermCode(semester);
        var filteredCourses = source.CoursesGrade
            .Where(c =>
                (string.IsNullOrWhiteSpace(academicYear) || c.AcademicYear == academicYear) &&
                (string.IsNullOrWhiteSpace(termCode) || c.Semester == termCode))
            .ToList();

        var grades = new ZdbkGrades
        {
            CoursesGrade = filteredCourses
        };

        grades.Credit = grades.CoursesGrade.Sum(x => x.Credit);
        if (grades.Credit > 0)
        {
            grades.GradePoint5 = grades.CoursesGrade.Sum(x => x.Grade5 * x.Credit) / grades.Credit;
            grades.GradePoint100 = grades.CoursesGrade.Sum(x => x.Grade100 * x.Credit) / grades.Credit;
            grades.MajorCredit = grades.Credit;
            grades.MajorGradePoint = grades.GradePoint5;
            grades.GradePoint4 = grades.GradePoint5 * 4 / 5;

        }

        return grades;
    }

    private static void EnsureCoursesCollection(ZdbkGrades grades)
    {
        grades.CoursesGrade ??= [];
    }
}


#region 调试使用

//[AutoInject(Lifetime.Singleton)]
public class DefaultGradeService : IZdbkGradeService
{
    public Task<Result<ZdbkGrades>> GetSemesterGradesAsync(string academicYear, string semester)
    {
        var result = new ZdbkGrades
        {
            Credit = 18.5,
            MajorCredit = 12.0,
            GradePoint5 = 4.2,
            GradePoint4 = 3.6,
            GradePoint100 = 88.5,
            MajorGradePoint = 4.5,
            CoursesGrade = new List<ZdbkCoursesGrade>
            {
                new ZdbkCoursesGrade { CourseName = "高级程序设计", CourseCode = "CS101", Grade100 = 95, Grade5 = 5.0, Credit = 4.0, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "线性代数", CourseCode = "MATH102", Grade100 = 82, Grade5 = 3.2, Credit = 3.5, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "大学物理", CourseCode = "PHYS103", Grade100 = 88, Grade5 = 3.8, Credit = 4.0, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "思想道德修养与法律基础", CourseCode = "MARX104", Grade100 = 91, Grade5 = 4.1, Credit = 3.0, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "体育(1)", CourseCode = "PE105", Grade100 = 85, Grade5 = 3.5, Credit = 1.0, Semester = "2023-2024-1" }
            }
        };

        return Task.FromResult(Result.Success(result));
    }


    public Task<Result<ZdbkGrades>> GetGradesAsync()
    {
        var result = new ZdbkGrades
        {
            Credit = 18.5,
            MajorCredit = 12.0,
            GradePoint5 = 4.2,
            GradePoint4 = 3.6,
            GradePoint100 = 88.5,
            MajorGradePoint = 4.5,
            CoursesGrade = new List<ZdbkCoursesGrade>
            {
                new ZdbkCoursesGrade { CourseName = "高级程序设计", CourseCode = "CS101", Grade100 = 95, Grade5 = 5.0, Credit = 4.0, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "线性代数", CourseCode = "MATH102", Grade100 = 82, Grade5 = 3.2, Credit = 3.5, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "大学物理", CourseCode = "PHYS103", Grade100 = 88, Grade5 = 3.8, Credit = 4.0, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "思想道德修养与法律基础", CourseCode = "MARX104", Grade100 = 91, Grade5 = 4.1, Credit = 3.0, Semester = "2023-2024-1" },
                new ZdbkCoursesGrade { CourseName = "体育(1)", CourseCode = "PE105", Grade100 = 85, Grade5 = 3.5, Credit = 1.0, Semester = "2023-2024-1" }
            }
        };

        return Task.FromResult(Result.Success(result));
    }

    public Task<Result<ZdbkGrades>> RefreshGradesAsync()
    {
        return GetGradesAsync();
    }

    public ZdbkGrades? GetCachedGrades()
    {
        return null;
    }
}
#endregion
