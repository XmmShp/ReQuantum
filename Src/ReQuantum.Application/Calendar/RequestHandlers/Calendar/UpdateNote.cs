using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Notes;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 更新便签
/// </summary>
public class UpdateNote(ICalendarNoteRepository repository, IUnitOfWork uow) : IRequestHandler<UpdateNoteRequest>
{
    public async Task<Result> HandleAsync(UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var existing = await repository.FindAsync(CalendarNoteId.Of(request.Id), cancellationToken);
        if (existing is null)
        {
            return Result.Fail(CalendarFailures.NoteNotFound);
        }

        existing.Update(request.Content);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
