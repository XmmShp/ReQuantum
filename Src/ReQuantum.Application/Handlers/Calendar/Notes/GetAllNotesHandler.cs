using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Notes;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Notes;

/// <summary>
/// 获取所有便签
/// </summary>
public class GetAllNotes : IRequestHandler<GetAllNotesRequest, GetAllNotesResponse>
{
    private readonly ICalendarNoteRepository _repository;

    public GetAllNotes(ICalendarNoteRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetAllNotesResponse>> HandleAsync(GetAllNotesRequest request, CancellationToken cancellationToken)
    {
        var notes = _repository.FindAll().Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetAllNotesResponse>>(new GetAllNotesResponse(notes));
    }
}
