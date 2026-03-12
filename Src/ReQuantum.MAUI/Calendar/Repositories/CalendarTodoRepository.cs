using Microsoft.EntityFrameworkCore;
using NOF.Annotation;
using NOF.Infrastructure.EntityFrameworkCore;
using ReQuantum.Domain.Calendar.AggregateRoots;
using ReQuantum.Domain.Calendar.Repositories;
using ReQuantum.MAUI.Persistence;

namespace ReQuantum.Calendar.Repositories;

[AutoInject(Lifetime.Scoped)]
public class CalendarTodoRepository : EFCoreRepository<CalendarTodo>, ICalendarTodoRepository
{
    private readonly ReQuantumMauiDbContext _dbContext;

    public CalendarTodoRepository(ReQuantumMauiDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public IAsyncEnumerable<CalendarTodo> FindByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return _dbContext.CalendarTodos
            .AsNoTracking()
            .OrderBy(t => t.DueTime)
            .Where(t => DateOnly.FromDateTime(t.DueTime) == date)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<CalendarTodo> FindByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return _dbContext.CalendarTodos
            .AsNoTracking()
            .OrderBy(t => t.DueTime)
            .Where(t =>
                DateOnly.FromDateTime(t.DueTime) >= startDate
                && DateOnly.FromDateTime(t.DueTime) <= endDate)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<CalendarTodo> FindIncompleteByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return _dbContext.CalendarTodos
            .AsNoTracking()
            .OrderBy(t => t.DueTime)
            .Where(t =>
                !t.IsCompleted
                && DateOnly.FromDateTime(t.DueTime) >= today
                && DateOnly.FromDateTime(t.DueTime) >= date)
            .AsAsyncEnumerable();
    }
}
