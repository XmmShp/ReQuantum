using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Notes;
using ReQuantum.Domain.Calendar.AggregateRoots;
using ReQuantum.Domain.Calendar;
using ReQuantum.Domain.Calendar.Repositories;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 添加便签
/// </summary>
public class AddNote(ICalendarNoteRepository repository, IUnitOfWork uow) : IRequestHandler<AddNoteRequest>
{
    public async Task<Result> HandleAsync(AddNoteRequest request, CancellationToken cancellationToken)
    {
        var note = CalendarNote.Create(request.Content);
        repository.Add(note);
        await uow.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
