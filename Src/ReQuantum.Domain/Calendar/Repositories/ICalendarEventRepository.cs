using NOF.Domain;
using ReQuantum.Domain.Calendar.AggregateRoots;

namespace ReQuantum.Domain.Calendar.Repositories;

/// <summary>
/// 日程事件仓储接口
/// </summary>
public interface ICalendarEventRepository : IRepository<CalendarEvent, CalendarEventId>
{
    IAsyncEnumerable<CalendarEvent> FindByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    IAsyncEnumerable<CalendarEvent> FindByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}
