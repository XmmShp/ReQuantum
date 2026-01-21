using System;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 解析后的课程信息 (从 kcb 字段解析而来)
/// </summary>
public class ParsedCourseInfo
{
    /// <summary>
    /// 课程名称
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// 教师姓名
    /// </summary>
    public string Teacher { get; set; } = string.Empty;

    /// <summary>
    /// 教室地点
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// 周次起始
    /// </summary>
    public int WeekStart { get; set; }

    /// <summary>
    /// 周次结束
    /// </summary>
    public int WeekEnd { get; set; }

    /// <summary>
    /// 考试日期
    /// </summary>
    public DateTime? ExamDate { get; set; }

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public TimeOnly? ExamStartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public TimeOnly? ExamEndTime { get; set; }

    /// <summary>
    /// 原始的 kcb 字段内容
    /// </summary>
    public string RawInfo { get; set; } = string.Empty;
}