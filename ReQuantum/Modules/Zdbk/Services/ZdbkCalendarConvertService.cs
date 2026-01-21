using Microsoft.Extensions.Logging;
using ReQuantum.Modules.Calendar.Entities;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Zdbk.Services;

/// <summary>
/// 教务网课程表到日历事件的转换服务
/// </summary>
public interface IZdbkCalendarConverter
{
    /// <summary>
    /// 将教务网课程列表转换为日历事件列表
    /// </summary>
    /// <param name="sections">课程列表</param>
    /// <param name="academicYear">学年（如 "2024-2025"）</param>
    /// <param name="semester">学期（如 "秋"、"冬"、"春"、"夏"）</param>
    /// <returns>日历事件列表</returns>
    Task<List<CalendarEvent>> ConvertToCalendarEventsAsync(
        IEnumerable<ZdbkSectionDto> sections,
        string academicYear,
        string semester);

    /// <summary>
    /// 将考试信息转换为日历事件列表
    /// </summary>
    /// <param name="exams">考试信息列表</param>
    /// <returns>日历事件列表</returns>
    List<CalendarEvent> ConvertExamsToCalendarEvents(List<ParsedExamInfo> exams);
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkCalendarConvertService : IZdbkCalendarConverter
{
    private readonly IAcademicCalendarService _calendarService;
    private readonly ILogger<ZdbkCalendarConvertService> _logger;

    public ZdbkCalendarConvertService(
        IAcademicCalendarService calendarService,
        ILogger<ZdbkCalendarConvertService> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task<List<CalendarEvent>> ConvertToCalendarEventsAsync(
        IEnumerable<ZdbkSectionDto> sections,
        string academicYear,
        string semester)
    {
        var events = new List<CalendarEvent>();
        var sectionList = sections.ToList();

        if (string.IsNullOrEmpty(academicYear) || string.IsNullOrEmpty(semester))
        {
            _logger.LogWarning("Academic year or semester is empty, cannot convert courses to calendar events");
            return events;
        }

        // 仅保留与目标学期匹配的课程（Term 包含学期关键字，例如："秋" 或 "冬"）
        var beforeFilter = sectionList.Count;
        sectionList = sectionList
            .Where(s => !string.IsNullOrWhiteSpace(s.Term) && s.Term.Contains(semester, StringComparison.Ordinal))
            .ToList();
        _logger.LogInformation("Filtered sections by semester '{Semester}': {Before} -> {After}", semester, beforeFilter, sectionList.Count);

        // 获取校历（用于调课和停课逻辑）
        var calendarResult = await _calendarService.GetCurrentCalendarAsync();

        if (!calendarResult.IsSuccess)
        {
            _logger.LogError("Failed to get calendar: {Message}", calendarResult.Message);
            return events;
        }

        var calendar = calendarResult.Value;

        if (calendar.AcademicYear != academicYear || GetSemesterCode(semester) != calendar.SemesterCode)
        {
            _logger.LogError(
                "Calendar mismatch: expected {Year}-{Semester}, got {CalendarYear}-{CalendarCode}",
                academicYear, semester, calendar.AcademicYear, calendar.SemesterCode);
            return events;
        }

        _logger.LogInformation(
            "Using calendar with {AdjustmentCount} adjustments and {SuspensionCount} suspension dates",
            calendar.CourseAdjustments.Count,
            calendar.ClassSuspensionDates.Count);

        // 计算学期起始日期
        var semesterStartDate = CalculateSemesterStartDate(calendar, semester);

        _logger.LogInformation("Converting {Count} sections for {Semester} semester with start date {StartDate}",
            sectionList.Count, semester, semesterStartDate);

        foreach (var section in sectionList)
        {
            try
            {
                var sectionEvents = ConvertSectionToEvents(section, semesterStartDate, calendar, semester);
                events.AddRange(sectionEvents);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert section {CourseId} to calendar events", section.CourseId);
            }
        }

        _logger.LogInformation("Successfully converted {SectionCount} sections to {EventCount} calendar events for {Semester}",
            sectionList.Count, events.Count, semester);

        return events;
    }

    /// <summary>
    /// 计算学期起始日期
    /// 秋/春学期：使用校历的StartDate
    /// 冬/夏学期：在大学期基础上加8周，并确保是周一
    /// </summary>
    private DateOnly CalculateSemesterStartDate(AcademicCalendar calendar, string semester)
    {
        if (semester is "秋" or "春")
        {
            _logger.LogDebug("Using calendar start date {StartDate} for {Semester}", calendar.StartDate, semester);
            return calendar.StartDate;
        }

        if (semester is "冬" or "夏")
        {
            var subSemesterStart = calendar.StartDate.AddDays(56);

            // 调整到周一
            var dayOfWeek = (int)subSemesterStart.DayOfWeek;
            if (dayOfWeek == 0) // 周日
            {
                subSemesterStart = subSemesterStart.AddDays(1);
            }
            else if (dayOfWeek != 1) // 不是周一
            {
                subSemesterStart = subSemesterStart.AddDays(1 - dayOfWeek);
            }

            _logger.LogInformation(
                "{Semester} start date: {StartDate} (calendar: {CalendarStart} + 8 weeks)",
                semester, subSemesterStart, calendar.StartDate);

            return subSemesterStart;
        }

        return default;
    }

    /// <summary>
    /// 获取学期代码（1或2）
    /// </summary>
    private static string GetSemesterCode(string semester)
    {
        return semester switch
        {
            "秋" => "1",
            "冬" => "1",
            "春" => "2",
            "夏" => "2",
            _ => "1"
        };
    }

    /// <summary>
    /// 将单个课程转换为日历事件列表
    /// </summary>
    /// <param name="section">课程信息</param>
    /// <param name="semesterStartDate">学期起始日期</param>
    /// <param name="calendar">校历信息（可选，用于调课和停课逻辑）</param>
    /// <param name="semester">学期名称（用于周次归零）</param>
    private static List<CalendarEvent> ConvertSectionToEvents(
        ZdbkSectionDto section,
        DateOnly semesterStartDate,
        AcademicCalendar? calendar,
        string semester)
    {
        var (courseInfo, startTime, endTime) = section.Parse();
        var events = new List<CalendarEvent>();

        for (var weekNumber = courseInfo.WeekStart; weekNumber <= courseInfo.WeekEnd; weekNumber++)
        {
            // 检查单双周
            if (!ShouldHaveCourseInWeek(weekNumber, int.Parse(section.WeekType)))
            {
                continue;
            }

            var originalDate = CalculateCourseDate(semesterStartDate, weekNumber, int.Parse(section.DayOfWeek));

            // 应用校历的调课和停课逻辑
            var actualDate = ApplyCalendarAdjustments(originalDate, calendar, out var shouldSkip);

            if (shouldSkip)
            {
                continue;
            }

            var eventId = $"{section.CourseId}_{semester}_{weekNumber}_{section.DayOfWeek}_{section.StartSection}".ToGuid();

            var startDateTime = actualDate.ToDateTime(startTime);
            var endDateTime = actualDate.ToDateTime(endTime);

            events.Add(new CalendarEvent
            {
                Id = eventId,
                Content = $"{courseInfo.CourseName}\n{courseInfo.Teacher}\n{courseInfo.Location}",
                StartTime = startDateTime,
                EndTime = endDateTime,
                CreatedAt = DateTime.Now
            });
        }

        return events;
    }

    /// <summary>
    /// 判断某周是否有课（根据周类型）
    /// </summary>
    private static bool ShouldHaveCourseInWeek(int weekNumber, int weekType)
    {
        return weekType switch
        {
            0 => weekNumber % 2 == 1, // 单周
            1 => weekNumber % 2 == 0, // 双周
            2 => true,                // 每周
            _ => false
        };
    }

    /// <summary>
    /// 应用校历的调课和停课逻辑
    /// </summary>
    /// <param name="originalDate">原始上课日期</param>
    /// <param name="calendar">校历信息</param>
    /// <param name="shouldSkip">是否应该跳过该日期（停课）</param>
    /// <returns>调整后的实际上课日期</returns>
    private static DateOnly ApplyCalendarAdjustments(
        DateOnly originalDate,
        AcademicCalendar? calendar,
        out bool shouldSkip)
    {
        shouldSkip = false;

        if (calendar == null)
        {
            return originalDate;
        }

        // 检查该日期是否停课
        if (calendar.IsSuspended(originalDate))
        {
            shouldSkip = true;
            return originalDate;
        }

        // 检查是否有调课
        var adjustmentFrom = calendar.CourseAdjustments
            .FirstOrDefault(a => a.OriginalDate == originalDate);

        if (adjustmentFrom != null)
        {
            if (calendar.IsSuspended(adjustmentFrom.TargetDate))
            {
                shouldSkip = true;
                return originalDate;
            }

            return adjustmentFrom.TargetDate;
        }

        return originalDate;
    }

    /// <summary>
    /// 根据学期起始日期、周次和星期几计算具体上课日期
    /// </summary>
    private static DateOnly CalculateCourseDate(DateOnly semesterStartDate, int weekNumber, int dayOfWeek)
    {
        var daysToAdd = (weekNumber - 1) * 7 + (dayOfWeek - 1);
        return semesterStartDate.AddDays(daysToAdd);
    }

    /// <summary>
    /// 将考试信息转换为日历事件
    /// </summary>
    public List<CalendarEvent> ConvertExamsToCalendarEvents(List<ParsedExamInfo> exams)
    {
        var events = new List<CalendarEvent>();

        foreach (var exam in exams)
        {
            // 跳过无考试和时间未确定的考试
            if (exam.ExamType == ExamType.NoExam || exam.StartTime == null || exam.EndTime == null)
            {
                continue;
            }

            var examTypeText = exam.ExamType == ExamType.MidTerm ? "期中考试" : "期末考试";
            var locationText = !string.IsNullOrEmpty(exam.Location)
                ? exam.Location
                : "地点待定";

            if (!string.IsNullOrEmpty(exam.Seat))
            {
                locationText += $" (座位号: {exam.Seat})";
            }

            var eventId = $"{exam.ClassId}_{exam.ExamType}_{exam.StartTime:yyyyMMddHHmm}".ToGuid();

            events.Add(new CalendarEvent
            {
                Id = eventId,
                Content = $"[务必核对!] {exam.CourseName} {examTypeText}\n学分: {exam.Credit:F1}",
                StartTime = exam.StartTime.Value,
                EndTime = exam.EndTime.Value,
                CreatedAt = DateTime.Now
            });
        }

        return events;
    }
}