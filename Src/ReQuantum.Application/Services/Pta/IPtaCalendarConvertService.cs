using ReQuantum.Application.Models.Pta;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Services.Pta;

public interface IPtaCalendarConvertService
{
    List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets);
}
