using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Notes;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarNoteDto = ReQuantum.Contract.Calendar.CalendarNote;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 获取所有便签
/// </summary>
public class GetAllNotes(ICalendarNoteRepository repository) : IRequestHandler<GetAllNotesRequest, GetAllNotesResponse>
{
    public async Task<Result<GetAllNotesResponse>> HandleAsync(GetAllNotesRequest request, CancellationToken cancellationToken)
    {
        var notes = new List<CalendarNoteDto>();
        await foreach (var note in repository.FindAllByCreatedAtDescendingAsync(cancellationToken))
        {
            notes.Add(note.Map.To<CalendarNoteDto>());
        }

        return new GetAllNotesResponse(notes);
    }
}
