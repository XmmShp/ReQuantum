using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Notes;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Notes;

/// <summary>
/// 删除便签
/// </summary>
public class DeleteNote : IRequestHandler<DeleteNoteRequest>
{
    private readonly ICalendarNoteRepository _repository;

    public DeleteNote(ICalendarNoteRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(DeleteNoteRequest request, CancellationToken cancellationToken)
    {
        _repository.Delete(CalendarNoteId.Of(request.Id));
        return Task.FromResult(Result.Success());
    }
}
