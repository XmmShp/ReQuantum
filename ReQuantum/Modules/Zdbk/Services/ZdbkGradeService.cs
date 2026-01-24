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
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Models;


namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkGradeService
{
    /// <param name="academicYear">学年（如 "2024-2025"）</param>
    /// <param name="semester">学期（如 "秋"、"冬"、"春"、"夏"）</param>
    Task<Result<ZdbkGrades>> GetSemeserGradesAsync(string academicYear, string semester);
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkGradeService : IZdbkGradeService
{
    private readonly IZdbkSessionService _sessionService;
    private readonly ILogger<ZdbkGradeService> _logger;

    private const string BaseUrl = "https://zdbk.zju.edu.cn/jwglxt";
    private const string GetGradeUrl = "/cxdy/xscjcx_cxXscjIndex.html";

    public ZdbkGradeService(
        IZdbkSessionService sessionService,
        ILogger<ZdbkGradeService> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
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

    public async Task<Result<ZdbkGrades>> GetSemeserGradesAsync(string academicYear, string semester)
    {
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
            if (string.IsNullOrEmpty(GetGradeUrl))
            {
                return Result.Fail("尚未配置成绩查询网址，请在 ZdbkGradeService 中补充 GetGradeUrl");
            }


            var requestUrl = $"{BaseUrl}{GetGradeUrl}?doType=query&gnmkdm=N508301&su={state.StudentId}";

            // 构造 POST 报文
            var formData = new Dictionary<string, string>
            {
                { "xn", academicYear ?? "" },
                { "xq", semester == "秋" || semester == "冬" ? "1" : "2"},
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
                }).ToList()

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

            return Result.Success(grades);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从 ZDBK 获取成绩时发生异常");
            return Result.Fail($"获取成绩失败: {ex.Message}");
        }

    }
}



//[AutoInject(Lifetime.Singleton)]
public class DefaultGradeService : IZdbkGradeService

{
    public Task<Result<ZdbkGrades>> GetSemeserGradesAsync(string academicYear, string semester)
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
}
