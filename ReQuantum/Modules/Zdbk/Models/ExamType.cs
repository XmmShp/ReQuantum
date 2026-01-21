using System;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 考试类型
/// </summary>
public enum ExamType
{
    /// <summary>
    /// 期中考试
    /// </summary>
    MidTerm,

    /// <summary>
    /// 期末考试
    /// </summary>
    FinalTerm,

    /// <summary>
    /// 无考试
    /// </summary>
    NoExam
}

/// <summary>
/// 解析到的考试信息
/// </summary>
public class ParsedExamInfo
{
    /// <summary>
    /// 选课课号（前22位）
    /// </summary>
    public string ClassId { get; set; } = string.Empty;

    /// <summary>
    /// 课程名称
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// 学分
    /// </summary>
    public float Credit { get; set; }

    /// <summary>
    /// 考试类型
    /// </summary>
    public ExamType ExamType { get; set; }

    /// <summary>
    /// 考试开始时间
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// 考试结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 考试地点
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 座位号
    /// </summary>
    public string? Seat { get; set; }

    /// <summary>
    /// 原始时间字符串（用于调试）
    /// </summary>
    public string? RawTimeString { get; set; }
}