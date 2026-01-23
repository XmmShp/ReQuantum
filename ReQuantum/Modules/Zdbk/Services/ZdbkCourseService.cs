using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Constants;
using ReQuantum.Modules.Zdbk.Enums;
using ReQuantum.Modules.Zdbk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkCourseService
{
    Task<Result<HashSet<SelectableCourse>>> GetAvailableCoursesAsync(CourseCategory category, int startPage, int endPage);
    Task<Result> UpdateSectionsAsync(SelectableCourse course);
    Task<Result> UpdateIntroductionAsync(Course course);
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkCourseService : IZdbkCourseService
{
    private readonly IZdbkSessionService _sessionService;
    private readonly ILogger<ZdbkCourseService> _logger;

    private const string BaseUrl = "https://zdbk.zju.edu.cn/jwglxt";
    private const string GetCourseUrl = "/xsxk/zzxkghb_cxZzxkGhbKcList.html";
    private const string GetSectionUrl = "/xsxk/zzxkghb_cxZzxkGhbJxbList.html";
    private const string GetIntroductionUrl = "/xkjjsc/kcjjck_cxXkjjPage.html";

    public ZdbkCourseService(
        IZdbkSessionService sessionService,
        ILogger<ZdbkCourseService> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<Result<HashSet<SelectableCourse>>> GetAvailableCoursesAsync(
        CourseCategory category,
        int startPage,
        int endPage)
    {
        var state = _sessionService.State;
        if (state == null)
        {
            return Result.Fail("未登录");
        }

        var clientResult = await _sessionService.GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }

        using var client = clientResult.Value;

        try
        {
            // 构造请求URL
            var requestUrl = $"{BaseUrl}{GetCourseUrl}?gnmkdm=N253530&su={state.StudentId}";

            // 获取课程类别参数
            var (dl, lx, xkmc) = CourseCategories.GetCourseType(category);

            // 构造POST数据
            var formData = new Dictionary<string, string>
            {
                { "dl", dl },
                { "lx", lx },
                { "nj", state.Grade ?? "" },
                { "xn", state.AcademicYear ?? "" },
                { "xq", state.Semester ?? "" },
                { "zydm", state.Major ?? "" },
                { "jxjhh", $"{state.Grade}{state.Major}" },
                { "xnxq", $"({state.AcademicYear}-{state.Semester})-" },
                { "kspage", startPage.ToString() },
                { "jspage", endPage.ToString() }
            };

            if (xkmc != null)
            {
                formData.Add("xkmc", xkmc);
            }

            var content = new FormUrlEncodedContent(formData);

            // 发送请求
            var response = await client.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode();

            // 解析响应
            var jsonContent = await response.Content.ReadAsStringAsync();
            var courseData = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

            if (courseData == null)
            {
                return Result.Fail("解析课程数据失败");
            }

            var courses = courseData.Select(json =>
            {
                // 解析课程信息字段 (格式: "信息~学分~周学时")
                var kcxxParts = json.GetProperty("kcxx").GetString()?.Split('~');
                var credits = kcxxParts?.Length > 1 && decimal.TryParse(kcxxParts[1], out var c) ? c : 0m;
                var weekTime = kcxxParts?.Length > 2 ? kcxxParts[2] : string.Empty;

                var course = new SelectableCourse
                {
                    Id = json.GetProperty("kcdm").GetString() ?? string.Empty,
                    Code = json.GetProperty("xkkh").GetString() ?? string.Empty,
                    Name = json.GetProperty("kcmc").GetString() ?? string.Empty,
                    Credits = credits,
                    WeekTime = weekTime,
                    Category = category,
                    Department = json.TryGetProperty("kkxy", out var kkxy) ? kkxy.GetString() ?? string.Empty : string.Empty,
                    Property = json.TryGetProperty("kcxz", out var kcxz) ? kcxz.GetString() ?? string.Empty : string.Empty,
                    Status = int.Parse(json.GetProperty("kcxzzt").GetString() ?? "0") is 1
                        ? CourseStatus.Selected
                        : CourseStatus.NotSelected,
                };
                return course;
            }).ToHashSet();

            _logger.LogInformation($"获取到 {courses.Count} 门 {category} 课程");
            return Result.Success(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可选课程失败");
            return Result.Fail($"获取可选课程失败: {ex.Message}");
        }
    }

    public async Task<Result> UpdateSectionsAsync(SelectableCourse course)
    {
        var state = _sessionService.State;
        if (state == null)
        {
            return Result.Fail("未登录");
        }

        var clientResult = await _sessionService.GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }

        using var client = clientResult.Value;

        try
        {
            // 构造请求URL
            var requestUrl = $"{BaseUrl}{GetSectionUrl}?gnmkdm=N253530&su={state.StudentId}";

            // 构造POST数据
            var formData = new Dictionary<string, string>
            {
                { "xn", state.AcademicYear ?? "" },
                { "xq", state.Semester ?? "" },
                { "xkkh", course.Code }
            };

            var content = new FormUrlEncodedContent(formData);

            // 发送请求
            var response = await client.PostAsync(requestUrl, content);
            response.EnsureSuccessStatusCode();

            // 解析响应
            var jsonContent = await response.Content.ReadAsStringAsync();
            var sectionData = JsonSerializer.Deserialize<List<JsonElement>>(jsonContent);

            if (sectionData == null)
            {
                return Result.Fail("解析教学班数据失败");
            }

            course.Sections.Clear();

            foreach (var json in sectionData)
            {
                // 解析容量信息 (格式: "余量/总容量")
                var capacityStr = json.TryGetProperty("rs", out var rs) ? rs.GetString() : null;
                var capacityParts = capacityStr?.Split('/');
                var availableSeats = capacityParts?.Length > 0 && int.TryParse(capacityParts[0], out var a) ? a : 0;
                var totalSeats = capacityParts?.Length > 1 && int.TryParse(capacityParts[1], out var t) ? t : 0;

                // 解析等待人数 (格式: "专业候补~总候补")
                var waitingStr = json.TryGetProperty("yxrs", out var yxrs) ? yxrs.GetString() : null;
                var waitingParts = waitingStr?.Split('~');
                var majorWaiting = waitingParts?.Length > 0 && int.TryParse(waitingParts[0], out var mw) ? mw : 0;
                var totalWaiting = waitingParts?.Length > 1 && int.TryParse(waitingParts[1], out var tw) ? tw : 0;

                var section = new SelectableSection
                {
                    Id = json.GetProperty("jxb_id").GetString() ?? string.Empty,
                    Course = course,
                    Instructors = json.TryGetProperty("jsxm", out var jsxm)
                        ? jsxm.GetString()?.Split(',').ToHashSet() ?? new HashSet<string>()
                        : new HashSet<string>(),
                    TeachingForm = json.TryGetProperty("jxfs", out var jxfs) ? jxfs.GetString() ?? string.Empty : string.Empty,
                    TeachingMethod = json.TryGetProperty("skfs", out var skfs) ? skfs.GetString() ?? string.Empty : string.Empty,
                    IsInternational = json.TryGetProperty("gjhkc", out var gjhkc) && gjhkc.GetString() == "1",
                    AvailableSeats = availableSeats,
                    Capacity = totalSeats,
                    MajorWaitingCount = majorWaiting,
                    TotalWaitingCount = totalWaiting
                };

                // 解析上课时间和地点
                var scheduleStr = json.TryGetProperty("sksj", out var sksj) ? sksj.GetString() : null;
                var locationStr = json.TryGetProperty("skdd", out var skdd) ? skdd.GetString() : null;

                if (!string.IsNullOrEmpty(scheduleStr) && !string.IsNullOrEmpty(locationStr))
                {
                    section.ScheduleAndLocations.Add((scheduleStr, locationStr));
                }

                // 解析考试时间
                var examTimeStr = json.TryGetProperty("kssj", out var kssj) ? kssj.GetString() : null;
                if (!string.IsNullOrEmpty(examTimeStr))
                {
                    try
                    {
                        section.ExamTime = TimeSlot.Parse(examTimeStr);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"解析考试时间失败: {examTimeStr}, {ex.Message}");
                    }
                }

                course.Sections.Add(section);
            }

            _logger.LogInformation($"课程 {course.Name} 获取到 {course.Sections.Count} 个教学班");
            return Result.Success("获取教学班信息成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取教学班信息失败");
            return Result.Fail($"获取教学班信息失败: {ex.Message}");
        }
    }

    public async Task<Result> UpdateIntroductionAsync(Course course)
    {
        var state = _sessionService.State;
        if (state == null)
        {
            return Result.Fail("未登录");
        }

        var clientResult = await _sessionService.GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }

        using var client = clientResult.Value;

        try
        {
            // 构造请求URL
            var requestUrl = $"{BaseUrl}{GetIntroductionUrl}?xkjjid={course.Id}&htmlType=kcjj&gnmkdm=N253530&su={state.StudentId}";

            // 发送请求
            var response = await client.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            // 解析响应
            var html = await response.Content.ReadAsStringAsync();

            // 使用正则表达式提取课程介绍
            var match = Regex.Match(html, @"<input[^>]*name=""xkjjHtml""[^>]*value=""([^""]*)""[^>]*>");
            if (match.Success)
            {
                var encodedIntro = match.Groups[1].Value;
                course.Introduction = HttpUtility.HtmlDecode(encodedIntro);
                _logger.LogInformation($"成功获取课程 {course.Name} 的介绍");
                return Result.Success("获取课程介绍成功");
            }

            return Result.Fail("未找到课程介绍");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取课程介绍失败");
            return Result.Fail($"获取课程介绍失败: {ex.Message}");
        }
    }
}