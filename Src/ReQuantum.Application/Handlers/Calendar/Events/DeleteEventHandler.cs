using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Events;

/// <summary>
/// 删除日程事件
/// </summary>
public class DeleteEvent : IRequestHandler<DeleteEventRequest>
{
    private readonly ICalendarEventRepository _repository;

    public DeleteEvent(ICalendarEventRepository repository)
    {
        _repository = repository;
    }

    public Task<Result> HandleAsync(DeleteEventRequest request, CancellationToken cancellationToken)
    {
        _repository.Delete(CalendarEventId.Of(request.Id));
        return Task.FromResult(Result.Success());
    }
}
