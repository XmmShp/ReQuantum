using ReQuantum.Application.Models.Calendar;
using ReQuantum.Application.Models.Zdbk;

namespace ReQuantum.Application.Services.Zdbk;

public interface IZdbkCalendarConverter
{
    Task<List<CalendarEvent>> ConvertToCalendarEventsAsync(
        IEnumerable<ZdbkSectionDto> sections,
        string academicYear,
        string semester);

    List<CalendarEvent> ConvertExamsToCalendarEvents(List<ParsedExamInfo> exams);
}
