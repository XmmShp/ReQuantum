using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 校历信息
/// </summary>
public class AcademicCalendar
{
    /// <summary>
    /// 当前学期名称（如 "2024-2025-1" 表示 2024-2025 学年第一学期）
    /// </summary>
    [JsonPropertyName("semester_name")]
    public required string SemesterName { get; set; }

    /// <summary>
    /// 学期开始日期
    /// </summary>
    [JsonPropertyName("start_date")]
    public required DateOnly StartDate { get; set; }

    /// <summary>
    /// 学期结束日期
    /// </summary>
    [JsonPropertyName("end_date")]
    public required DateOnly EndDate { get; set; }

    /// <summary>
    /// 调课时间列表
    /// </summary>
    [JsonPropertyName("course_adjustments")]
    public List<CourseAdjustment> CourseAdjustments { get; set; } = [];

    /// <summary>
    /// 停课日期列表
    /// </summary>
    [JsonPropertyName("class_suspension_dates")]
    public List<DateOnly> ClassSuspensionDates { get; set; } = [];

    /// <summary>
    /// 校历版本号
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; set; }

    /// <summary>
    /// 是否为短学期（夏季小学期）
    /// </summary>
    [JsonPropertyName("is_short_semester")]
    public bool IsShortSemester { get; set; }

    /// <summary>
    /// 学年（如 "2024-2025"）
    /// </summary>
    [JsonIgnore]
    public string AcademicYear
    {
        get
        {
            var parts = SemesterName.Split('-');
            return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : string.Empty;
        }
    }

    /// <summary>
    /// 学期代码（1 或 2）
    /// </summary>
    [JsonIgnore]
    public string SemesterCode
    {
        get
        {
            var parts = SemesterName.Split('-');
            return parts.Length >= 3 ? parts[2] : "1";
        }
    }

    /// <summary>
    /// 获取指定日期对应的周次（从1开始）
    /// </summary>
    /// <param name="date">指定日期</param>
    /// <returns>周次，如果不在学期内返回 null</returns>
    public int? GetWeekNumber(DateOnly date)
    {
        if (date < StartDate || date > EndDate)
        {
            return null;
        }

        // 计算原始周次
        var daysDiff = date.DayNumber - StartDate.DayNumber;
        var weekNumber = (daysDiff / 7) + 1;

        return weekNumber;
    }

    /// <summary>
    /// 获取指定周次对应的学期名称（秋/冬/春/夏）
    /// </summary>
    /// <param name="weekNumber">周次</param>
    /// <returns>学期名称</returns>
    public string GetSemesterNameForWeek(int weekNumber)
    {
        if (IsShortSemester)
        {
            return "夏"; // 短学期统一为夏
        }

        // 非短学期：1-8周为秋/春，9-16周为冬/夏
        if (SemesterCode == "1") // 第一学期
        {
            return weekNumber <= 8 ? "秋" : "冬";
        }
        else // 第二学期
        {
            return weekNumber <= 8 ? "春" : "夏";
        }
    }

    /// <summary>
    /// 检查指定日期是否被调课
    /// </summary>
    /// <param name="date">指定日期</param>
    /// <returns>调课信息，如果没有调课返回 null</returns>
    public CourseAdjustment? GetAdjustment(DateOnly date)
    {
        return CourseAdjustments.FirstOrDefault(a => a.OriginalDate == date);
    }

    /// <summary>
    /// 检查指定日期是否停课
    /// </summary>
    /// <param name="date">指定日期</param>
    /// <returns>是否停课</returns>
    public bool IsSuspended(DateOnly date)
    {
        return ClassSuspensionDates.Contains(date);
    }

    /// <summary>
    /// 获取调整后的实际上课日期
    /// </summary>
    /// <param name="date">原始日期</param>
    /// <returns>实际上课日期</returns>
    public DateOnly GetActualCourseDate(DateOnly date)
    {
        // 检查是否有其他日期调整到这一天
        var adjustmentToThisDate = CourseAdjustments.FirstOrDefault(a => a.TargetDate == date);
        if (adjustmentToThisDate != null)
        {
            return adjustmentToThisDate.OriginalDate;
        }

        // 检查这一天是否被调整到其他日期
        var adjustmentFromThisDate = CourseAdjustments.FirstOrDefault(a => a.OriginalDate == date);
        if (adjustmentFromThisDate != null)
        {
            return adjustmentFromThisDate.TargetDate;
        }

        // 没有调整
        return date;
    }
}

/// <summary>
/// 调课信息
/// </summary>
public class CourseAdjustment
{
    /// <summary>
    /// 原始日期（被调整的日期）
    /// </summary>
    [JsonPropertyName("original_date")]
    public required DateOnly OriginalDate { get; set; }

    /// <summary>
    /// 目标日期（调整到的日期）
    /// </summary>
    [JsonPropertyName("target_date")]
    public required DateOnly TargetDate { get; set; }

    /// <summary>
    /// 调课原因（可选）
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// 校历响应
/// </summary>
public class AcademicCalendarResponse
{
    /// <summary>
    /// 成功标志
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// 错误消息（如果有）
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// 校历数据
    /// </summary>
    [JsonPropertyName("data")]
    public AcademicCalendar? Data { get; set; }
}