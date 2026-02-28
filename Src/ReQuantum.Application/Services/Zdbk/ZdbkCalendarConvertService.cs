using Microsoft.Extensions.Logging;
using ReQuantum.Application.Models.Calendar;
using ReQuantum.Application.Models.Zdbk;
using ReQuantum.Application.Utilities;

namespace ReQuantum.Application.Services.Zdbk;

public class ZdbkCalendarConvertService : IZdbkCalendarConverter
{
    private readonly IAcademicCalendarService _calendarService;
    private readonly ILogger<ZdbkCalendarConvertService> _logger;

    public ZdbkCalendarConvertService(IAcademicCalendarService calendarService, ILogger<ZdbkCalendarConvertService> logger)
    {
        _calendarService = calendarService;
        _logger = logger;
    }

    public async Task<List<CalendarEvent>> ConvertToCalendarEventsAsync(
        IEnumerable<ZdbkSectionDto> sections, string academicYear, string semester)
    {
        var events = new List<CalendarEvent>();
        var sectionList = sections.ToList();

        if (string.IsNullOrEmpty(academicYear) || string.IsNullOrEmpty(semester))
            return events;

        sectionList = sectionList
            .Where(s => !string.IsNullOrWhiteSpace(s.Term) && s.Term.Contains(semester, StringComparison.Ordinal))
            .ToList();

        var calendarResult = await _calendarService.GetCurrentCalendarAsync();
        if (!calendarResult.IsSuccess) return events;

        var calendar = calendarResult.Value!;
        if (calendar.AcademicYear != academicYear || GetSemesterCode(semester) != calendar.SemesterCode)
            return events;

        var semesterStartDate = CalculateSemesterStartDate(calendar, semester);

        foreach (var section in sectionList)
        {
            try
            {
                var sectionEvents = ConvertSectionToEvents(section, semesterStartDate, calendar, semester);
                events.AddRange(sectionEvents);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to convert section {CourseId}", section.CourseId);
            }
        }

        return events;
    }

    private static DateOnly CalculateSemesterStartDate(AcademicCalendar calendar, string semester)
    {
        if (semester is "秋" or "春") return calendar.StartDate;

        if (semester is "冬" or "夏")
        {
            var subSemesterStart = calendar.StartDate.AddDays(56);
            var dayOfWeek = (int)subSemesterStart.DayOfWeek;
            if (dayOfWeek == 0) subSemesterStart = subSemesterStart.AddDays(1);
            else if (dayOfWeek != 1) subSemesterStart = subSemesterStart.AddDays(1 - dayOfWeek);
            return subSemesterStart;
        }

        return default;
    }

    private static string GetSemesterCode(string semester) => semester switch
    {
        "秋" or "冬" => "1",
        "春" or "夏" => "2",
        _ => "1"
    };

    private static List<CalendarEvent> ConvertSectionToEvents(
        ZdbkSectionDto section, DateOnly semesterStartDate, AcademicCalendar? calendar, string semester)
    {
        var (courseInfo, startTime, endTime) = section.Parse();
        var events = new List<CalendarEvent>();

        for (var weekNumber = courseInfo.WeekStart; weekNumber <= courseInfo.WeekEnd; weekNumber++)
        {
            if (!ShouldHaveCourseInWeek(weekNumber, int.Parse(section.WeekType)))
                continue;

            var originalDate = CalculateCourseDate(semesterStartDate, weekNumber, int.Parse(section.DayOfWeek));
            var actualDate = ApplyCalendarAdjustments(originalDate, calendar, out var shouldSkip);
            if (shouldSkip) continue;

            var eventId = $"{section.CourseId}_{semester}_{weekNumber}_{section.DayOfWeek}_{section.StartSection}".ToGuid();

            events.Add(new CalendarEvent
            {
                Id = eventId,
                Content = $"{courseInfo.CourseName}\n{courseInfo.Teacher}\n{courseInfo.Location}",
                StartTime = actualDate.ToDateTime(startTime),
                EndTime = actualDate.ToDateTime(endTime),
                CreatedAt = DateTime.Now
            });
        }

        return events;
    }

    private static bool ShouldHaveCourseInWeek(int weekNumber, int weekType) => weekType switch
    {
        0 => weekNumber % 2 == 1,
        1 => weekNumber % 2 == 0,
        2 => true,
        _ => false
    };

    private static DateOnly ApplyCalendarAdjustments(DateOnly originalDate, AcademicCalendar? calendar, out bool shouldSkip)
    {
        shouldSkip = false;
        if (calendar == null) return originalDate;

        if (calendar.IsSuspended(originalDate)) { shouldSkip = true; return originalDate; }

        var adjustmentFrom = calendar.CourseAdjustments.FirstOrDefault(a => a.OriginalDate == originalDate);
        if (adjustmentFrom != null)
        {
            if (calendar.IsSuspended(adjustmentFrom.TargetDate)) { shouldSkip = true; return originalDate; }
            return adjustmentFrom.TargetDate;
        }

        return originalDate;
    }

    private static DateOnly CalculateCourseDate(DateOnly semesterStartDate, int weekNumber, int dayOfWeek) =>
        semesterStartDate.AddDays((weekNumber - 1) * 7 + (dayOfWeek - 1));

    public List<CalendarEvent> ConvertExamsToCalendarEvents(List<ParsedExamInfo> exams)
    {
        var events = new List<CalendarEvent>();
        foreach (var exam in exams)
        {
            if (exam.ExamType == ExamType.NoExam || exam.StartTime == null || exam.EndTime == null) continue;

            var examTypeText = exam.ExamType == ExamType.MidTerm ? "期中考试" : "期末考试";
            var locationText = !string.IsNullOrEmpty(exam.Location) ? exam.Location : "地点待定";
            if (!string.IsNullOrEmpty(exam.Seat)) locationText += $" (座位号: {exam.Seat})";

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
