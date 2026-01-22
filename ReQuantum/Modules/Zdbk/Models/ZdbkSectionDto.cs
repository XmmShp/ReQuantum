using System;
using System.Text.Json.Serialization;
using ReQuantum.Modules.Zdbk.Constants;
using ReQuantum.Modules.Zdbk.Parsers;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 教务网单个课程信息
/// </summary>
public class ZdbkSectionDto
{
    /// <summary>
    /// 选课课号（唯一标识）
    /// </summary>
    [JsonPropertyName("xkkh")]
    public string CourseId { get; set; } = string.Empty;

    /// <summary>
    /// 课程完整信息（HTML 格式，需要解析）
    /// 格式：课程名<br>周次信息<br>教师<br>教室+考试时间
    /// </summary>
    [JsonPropertyName("kcb")]
    public string CourseInfo { get; set; } = string.Empty;

    /// <summary>
    /// 星期几 (1-7, 1=周一)
    /// </summary>
    [JsonPropertyName("xqj")]
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>
    /// 起始节次 (1-13)
    /// </summary>
    [JsonPropertyName("djj")]
    public string StartSection { get; set; } = string.Empty;

    /// <summary>
    /// 上课长度（持续节数）
    /// </summary>
    [JsonPropertyName("skcd")]
    public string Duration { get; set; } = string.Empty;

    /// <summary>
    /// 单双周标记
    /// 0 = 单周, 1 = 双周, 2 = 每周
    /// </summary>
    [JsonPropertyName("dsz")]
    public string WeekType { get; set; } = string.Empty;

    /// <summary>
    /// 学期（如 "秋冬", "春夏"）
    /// </summary>
    [JsonPropertyName("xxq")]
    public string Term { get; set; } = string.Empty;

    /// <summary>
    /// 教师工号
    /// </summary>
    [JsonPropertyName("jszgh")]
    public string? TeacherId { get; set; }

    /// <summary>
    /// 解析课程的 HTML 信息，并返回包含完整信息的元组
    /// </summary>
    public (ParsedCourseInfo Info, TimeOnly StartTime, TimeOnly EndTime) Parse()
    {
        var courseInfo = CourseInfoParser.Parse(CourseInfo);
        var (startTime, endTime) = ClassTimeTable.GetClassTime(int.Parse(StartSection), int.Parse(Duration));
        return (courseInfo, startTime, endTime);
    }
}
