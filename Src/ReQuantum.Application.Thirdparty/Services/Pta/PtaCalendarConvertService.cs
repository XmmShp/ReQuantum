using NOF.Annotation;
using ReQuantum.Application.Models.Pta;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.AggregateRoots;

namespace ReQuantum.Application.Services.Pta;

[AutoInject(Lifetime.Singleton)]
public class PtaCalendarConvertService : IPtaCalendarConvertService
{
    public List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets)
    {
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        return problemSets
            .Where(ps => ps.EndAt > thirtyDaysAgo)
            .Select(ps => CalendarEvent.CreateFromSource(
                $"{ps.Name}的DDL",
                ps.EndAt,
                ps.EndAt,
                CalendarEventSource.Pta,
                "PTA"))
            .ToList();
    }
}
