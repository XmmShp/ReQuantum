using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Notes;
using DomainCalendarNote = ReQuantum.Domain.Calendar.CalendarNote;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Notes;

/// <summary>
/// 添加或更新便签
/// </summary>
public class AddOrUpdateNote : IRequestHandler<AddOrUpdateNoteRequest>
{
    private readonly ICalendarNoteRepository _repository;

    public AddOrUpdateNote(ICalendarNoteRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(AddOrUpdateNoteRequest request, CancellationToken cancellationToken)
    {
        if (request.Id.HasValue)
        {
            var existing = _repository.FindById(CalendarNoteId.Of(request.Id.Value));
            if (existing is not null)
            {
                existing.Update(request.Content);
                _repository.AddOrUpdate(existing);
                return Task.FromResult(Result.Success());
            }
        }

        var note = DomainCalendarNote.Create(request.Content);
        _repository.AddOrUpdate(note);
        return Task.FromResult(Result.Success());
    }
}
