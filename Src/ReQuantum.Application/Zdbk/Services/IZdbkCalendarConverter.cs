using ReQuantum.Application.Zdbk.Models;
using ReQuantum.Domain.Calendar.AggregateRoots;

namespace ReQuantum.Application.Zdbk.Services;

public interface IZdbkCalendarConverter
{
    Task<List<CalendarEvent>> ConvertToCalendarEventsAsync(
        IEnumerable<ZdbkSectionDto> sections,
        string academicYear,
        string semester);

    List<CalendarEvent> ConvertExamsToCalendarEvents(List<ParsedExamInfo> exams);
}
