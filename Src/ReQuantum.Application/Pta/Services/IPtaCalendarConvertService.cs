using ReQuantum.Application.Pta.Models;
using ReQuantum.Domain.Calendar.AggregateRoots;

namespace ReQuantum.Application.Pta.Services;

public interface IPtaCalendarConvertService
{
    List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets);
}
