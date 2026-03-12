using Microsoft.EntityFrameworkCore;
using NOF.Annotation;
using NOF.Infrastructure.EntityFrameworkCore;
using ReQuantum.Domain.Calendar.AggregateRoots;
using ReQuantum.Domain.Calendar.Repositories;
using ReQuantum.MAUI.Persistence;

namespace ReQuantum.Calendar.Repositories;

[AutoInject(Lifetime.Scoped)]
public class CalendarNoteRepository : EFCoreRepository<CalendarNote>, ICalendarNoteRepository
{
    private readonly ReQuantumMauiDbContext _dbContext;

    public CalendarNoteRepository(ReQuantumMauiDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public IAsyncEnumerable<CalendarNote> FindAllByCreatedAtDescendingAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.CalendarNotes
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .AsAsyncEnumerable();
    }
}
