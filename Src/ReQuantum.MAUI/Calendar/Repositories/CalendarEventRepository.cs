using Microsoft.EntityFrameworkCore;
using NOF.Annotation;
using NOF.Infrastructure.EntityFrameworkCore;
using ReQuantum.Domain.Calendar.AggregateRoots;
using ReQuantum.Domain.Calendar.Repositories;
using ReQuantum.MAUI.Persistence;

namespace ReQuantum.Calendar.Repositories;

[AutoInject(Lifetime.Scoped)]
public class CalendarEventRepository : EFCoreRepository<CalendarEvent>, ICalendarEventRepository
{
    private readonly ReQuantumMauiDbContext _dbContext;
    public CalendarEventRepository(ReQuantumMauiDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public IAsyncEnumerable<CalendarEvent> FindByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        return _dbContext.CalendarEvents
            .OrderBy(e => e.StartTime)
            .Where(e =>
                DateOnly.FromDateTime(e.StartTime) <= date
                && DateOnly.FromDateTime(e.EndTime) >= date)
            .AsAsyncEnumerable();
    }

    public IAsyncEnumerable<CalendarEvent> FindByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default)
    {
        return _dbContext.CalendarEvents
            .OrderBy(e => e.StartTime)
            .Where(e =>
                DateOnly.FromDateTime(e.StartTime) <= endDate
                && DateOnly.FromDateTime(e.EndTime) >= startDate)
            .AsAsyncEnumerable();
    }
}
