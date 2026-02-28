using ReQuantum.Application.Models.Calendar;
using ReQuantum.Application.Models.Pta;

namespace ReQuantum.Application.Services.Pta;

public interface IPtaCalendarConvertService
{
    List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets);
}
