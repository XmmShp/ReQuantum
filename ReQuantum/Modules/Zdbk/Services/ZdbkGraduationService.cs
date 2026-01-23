using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Enums;
using ReQuantum.Modules.Zdbk.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkGraduationService
{
    Task<Result<HashSet<StatefulCourse>>> RefreshGraduationRequirementsAsync();
    HashSet<StatefulCourse>? GraduationRequirements { get; }
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkGraduationService : IZdbkGraduationService
{
    private readonly IZdbkSessionService _sessionService;
    private readonly IStorage _storage;
    private readonly ILogger<ZdbkGraduationService> _logger;

    private const string BaseUrl = "https://zdbk.zju.edu.cn/jwglxt";
    private const string GetGraduationAuditUrl = "/bysh/byshck_cxByshzsIndex.html";
    private const string StorageKey = "Zdbk:GraduationRequirements";

    private HashSet<StatefulCourse>? _graduationRequirements;

    public HashSet<StatefulCourse>? GraduationRequirements => _graduationRequirements;

    public ZdbkGraduationService(
        IZdbkSessionService sessionService,
        IStorage storage,
        ILogger<ZdbkGraduationService> logger)
    {
        _sessionService = sessionService;
        _storage = storage;
        _logger = logger;

        // 加载缓存
        LoadGraduationRequirements();
    }

    public async Task<Result<HashSet<StatefulCourse>>> RefreshGraduationRequirementsAsync()
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
            var requestUrl = $"{BaseUrl}{GetGraduationAuditUrl}?gnmkdm=N305508&su={state.StudentId}";

            // 发送请求
            var response = await client.GetAsync(requestUrl);
            response.EnsureSuccessStatusCode();

            // 解析HTML
            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var courses = new HashSet<StatefulCourse>();

            // 查找所有课程表格行
            // 通常毕业审核页面有多个表格，每个表格对应一个课程类别
            var tables = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table')]");
            if (tables == null)
            {
                _logger.LogWarning("未找到毕业审核表格");
                return Result.Fail("未找到毕业审核表格");
            }

            foreach (var table in tables)
            {
                // 获取表格类别标题
                var categoryNode = table.SelectSingleNode(".//preceding-sibling::*[contains(@class, 'panel-heading') or contains(@class, 'title')][1]");
                var categoryName = categoryNode?.InnerText.Trim() ?? "未分类";

                var rows = table.SelectNodes(".//tbody/tr");
                if (rows == null) continue;

                foreach (var row in rows)
                {
                    try
                    {
                        var cells = row.SelectNodes(".//td");
                        if (cells == null || cells.Count < 3) continue;

                        // 解析课程信息（表格结构可能因学校而异，需要根据实际情况调整）
                        var courseName = cells[0]?.InnerText.Trim() ?? string.Empty;
                        var courseId = cells[1]?.InnerText.Trim() ?? string.Empty;
                        var creditsStr = cells[2]?.InnerText.Trim();
                        var statusStr = cells.Count > 3 ? cells[3]?.InnerText.Trim() : null;

                        if (string.IsNullOrEmpty(courseName)) continue;

                        // 解析学分
                        var credits = creditsStr != null && decimal.TryParse(creditsStr, out var c) ? c : 0m;

                        // 解析状态
                        var status = ParseCourseStatus(statusStr);

                        // 推断课程类别
                        var category = InferCourseCategory(categoryName);

                        var course = new StatefulCourse
                        {
                            Id = courseId,
                            Name = courseName,
                            Credits = credits,
                            Status = status,
                            Category = category,
                            Department = categoryName
                        };

                        courses.Add(course);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "解析单个毕业要求课程失败，跳过");
                    }
                }
            }

            // 更新缓存
            _graduationRequirements = courses;
            SaveGraduationRequirements();

            _logger.LogInformation($"成功获取 {courses.Count} 门毕业要求课程");
            return Result.Success(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取毕业审核信息失败");
            return Result.Fail($"获取毕业审核信息失败: {ex.Message}");
        }
    }

    private CourseStatus ParseCourseStatus(string? statusStr)
    {
        if (string.IsNullOrEmpty(statusStr)) return CourseStatus.Unknown;

        statusStr = statusStr.Trim();

        return statusStr switch
        {
            "已通过" or "已修读" or "已获得" or "合格" => CourseStatus.Passed,
            "未通过" or "不合格" or "未获得" => CourseStatus.Failed,
            "已选" or "在修" => CourseStatus.Selected,
            "未选" or "未修读" => CourseStatus.NotSelected,
            _ => CourseStatus.Unknown
        };
    }

    private CourseCategory InferCourseCategory(string categoryName)
    {
        // 根据类别名称推断课程类别
        if (categoryName.Contains("通识必修")) return CourseCategory.CompulsoryAll;
        if (categoryName.Contains("思政")) return CourseCategory.CompulsoryIpm;
        if (categoryName.Contains("外语")) return CourseCategory.CompulsoryLan;
        if (categoryName.Contains("计算机")) return CourseCategory.CompulsoryCom;
        if (categoryName.Contains("创新创业")) return CourseCategory.CompulsoryEtp;
        if (categoryName.Contains("自然科学")) return CourseCategory.CompulsorySci;

        if (categoryName.Contains("通识选修")) return CourseCategory.ElectiveAll;
        if (categoryName.Contains("中华文化")) return CourseCategory.ElectiveChC;
        if (categoryName.Contains("世界文明")) return CourseCategory.ElectiveGlC;
        if (categoryName.Contains("当代社会")) return CourseCategory.ElectiveSoc;
        if (categoryName.Contains("科技创新")) return CourseCategory.ElectiveSci;
        if (categoryName.Contains("文艺审美")) return CourseCategory.ElectiveArt;
        if (categoryName.Contains("生命探索")) return CourseCategory.ElectiveBio;
        if (categoryName.Contains("博雅教育")) return CourseCategory.ElectiveTec;

        if (categoryName.Contains("专业基础")) return CourseCategory.MajorFundation;
        if (categoryName.Contains("专业")) return CourseCategory.MyMajor;

        if (categoryName.Contains("体育")) return CourseCategory.PhysicalEdu;
        if (categoryName.Contains("认定")) return CourseCategory.AccreditedAll;
        if (categoryName.Contains("国际")) return CourseCategory.International;
        if (categoryName.Contains("荣誉")) return CourseCategory.Honor;

        return CourseCategory.Undefined;
    }

    private void LoadGraduationRequirements()
    {
        if (_storage.TryGet<HashSet<StatefulCourse>>(StorageKey, out var requirements) && requirements != null)
        {
            _graduationRequirements = requirements;
            _logger.LogInformation($"从缓存加载 {requirements.Count} 门毕业要求课程");
        }
    }

    private void SaveGraduationRequirements()
    {
        if (_graduationRequirements != null)
        {
            _storage.Set(StorageKey, _graduationRequirements);
            _logger.LogDebug("毕业要求已保存到缓存");
        }
    }
}
