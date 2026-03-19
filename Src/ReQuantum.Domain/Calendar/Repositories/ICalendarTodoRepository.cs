using NOF.Domain;
using ReQuantum.Domain.Calendar.AggregateRoots;

namespace ReQuantum.Domain.Calendar.Repositories;

/// <summary>
/// 待办事项仓储接口
/// </summary>
public interface ICalendarTodoRepository : IRepository<CalendarTodo, CalendarTodoId>
{
    IAsyncEnumerable<CalendarTodo> FindByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    IAsyncEnumerable<CalendarTodo> FindByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    IAsyncEnumerable<CalendarTodo> FindIncompleteByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<CalendarTodo?> FindByExternalAsync(string externalSource, string externalId, CancellationToken cancellationToken = default);
}
