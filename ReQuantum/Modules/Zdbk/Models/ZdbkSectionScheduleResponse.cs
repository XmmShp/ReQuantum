using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 教务网课程表响应
/// </summary>
public class ZdbkSectionScheduleResponse
{
    /// <summary>
    /// 课程列表
    /// </summary>
    [JsonPropertyName("kbList")]
    public List<ZdbkSectionDto> SectionList { get; set; } = [];

    /// <summary>
    /// 学年名
    /// </summary>
    [JsonPropertyName("xnm")]
    public string? AcademicYear { get; set; }

    /// <summary>
    /// 学期名
    /// </summary>
    [JsonPropertyName("xqm")]
    public string? Semester { get; set; }

    /// <summary>
    /// 相关学期（用于本地合并结果，不来自服务器）
    /// </summary>
    [JsonIgnore]
    public string[]? RelatedSemesters { get; set; }

    /// <summary>
    /// 学号
    /// </summary>
    [JsonPropertyName("xh")]
    public string? StudentId { get; set; }

    /// <summary>
    /// 学生姓名
    /// </summary>
    [JsonPropertyName("xm")]
    public string? StudentName { get; set; }

    /// <summary>
    /// 行政班
    /// </summary>
    [JsonPropertyName("xzb")]
    public string? AdministrativeClass { get; set; }

    /// <summary>
    /// 学院
    /// </summary>
    [JsonPropertyName("xy")]
    public string? College { get; set; }

    /// <summary>
    /// 专业
    /// </summary>
    [JsonPropertyName("zy")]
    public string? Major { get; set; }
}
