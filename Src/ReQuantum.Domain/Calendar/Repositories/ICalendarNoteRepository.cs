using NOF.Domain;
using ReQuantum.Domain.Calendar.AggregateRoots;

namespace ReQuantum.Domain.Calendar.Repositories;

/// <summary>
/// 便签仓储接口
/// </summary>
public interface ICalendarNoteRepository : IRepository<CalendarNote, CalendarNoteId>
{
    IAsyncEnumerable<CalendarNote> FindAllByCreatedAtDescendingAsync(CancellationToken cancellationToken = default);
}
