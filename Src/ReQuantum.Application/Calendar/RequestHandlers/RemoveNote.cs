using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Notes;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 删除便签
/// </summary>
public class RemoveNote(ICalendarNoteRepository repository, IUnitOfWork uow) : IRequestHandler<RemoveNoteRequest>
{
    public async Task<Result> HandleAsync(RemoveNoteRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindAsync(CalendarNoteId.Of(request.Id), cancellationToken);
        if (existing is null)
        {
            return Result.Fail(CalendarFailures.NoteNotFound);
        }

        repository.Remove(existing);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
