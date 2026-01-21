using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 教务网考试信息（原始数据）
/// </summary>
public class ZdbkExamDto
{
    /// <summary>
    /// 选课课号
    /// </summary>
    [JsonPropertyName("xkkh")]
    public string CourseId { get; set; } = string.Empty;

    /// <summary>
    /// 课程名称
    /// </summary>
    [JsonPropertyName("kcmc")]
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// 学分
    /// </summary>
    [JsonPropertyName("xf")]
    public string Credit { get; set; } = string.Empty;

    /// <summary>
    /// 期末考试时间（如 "2024年06月28日(08:00-10:00)" 或 "冬考试第2天(10:30-12:30)"）
    /// </summary>
    [JsonPropertyName("kssj")]
    public string? FinalExamTime { get; set; }

    /// <summary>
    /// 期末考试教室
    /// </summary>
    [JsonPropertyName("jsmc")]
    public string? FinalExamLocation { get; set; }

    /// <summary>
    /// 期末考试座位号
    /// </summary>
    [JsonPropertyName("zwxh")]
    public string? FinalExamSeat { get; set; }

    /// <summary>
    /// 期中考试时间
    /// </summary>
    [JsonPropertyName("qzkssj")]
    public string? MidTermExamTime { get; set; }

    /// <summary>
    /// 期中考试教室
    /// </summary>
    [JsonPropertyName("qzjsmc")]
    public string? MidTermExamLocation { get; set; }

    /// <summary>
    /// 期中考试座位号
    /// </summary>
    [JsonPropertyName("qzzwxh")]
    public string? MidTermExamSeat { get; set; }
}

/// <summary>
/// 教务网考试信息API响应
/// </summary>
public class ZdbkExamResponse
{
    /// <summary>
    /// 考试列表
    /// </summary>
    [JsonPropertyName("items")]
    public List<ZdbkExamDto> Items { get; set; } = [];
}
