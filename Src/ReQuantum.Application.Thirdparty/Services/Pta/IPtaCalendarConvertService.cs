using ReQuantum.Application.Models.Pta;
using ReQuantum.Domain.Calendar.AggregateRoots;

namespace ReQuantum.Application.Services.Pta;

public interface IPtaCalendarConvertService
{
    List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets);
}
