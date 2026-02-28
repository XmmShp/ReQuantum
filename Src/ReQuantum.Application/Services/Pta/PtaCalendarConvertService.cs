using ReQuantum.Application.Models.Calendar;
using ReQuantum.Application.Models.Pta;

namespace ReQuantum.Application.Services.Pta;

public class PtaCalendarConvertService : IPtaCalendarConvertService
{
    public List<CalendarEvent> ConvertToCalendarEvents(List<PtaProblemSet> problemSets)
    {
        var thirtyDaysAgo = DateTime.Now.AddDays(-30);
        return problemSets
            .Where(ps => ps.EndAt > thirtyDaysAgo)
            .Select(ps => new CalendarEvent
            {
                Content = $"{ps.Name}的DDL",
                StartTime = ps.EndAt,
                EndTime = ps.EndAt,
                IsFromPta = true,
                From = "PTA"
            })
            .ToList();
    }
}
