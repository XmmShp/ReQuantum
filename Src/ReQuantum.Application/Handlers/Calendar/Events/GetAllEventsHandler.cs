using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Events;

/// <summary>
/// 获取所有日程事件
/// </summary>
public class GetAllEvents : IRequestHandler<GetAllEventsRequest, GetAllEventsResponse>
{
    private readonly ICalendarEventRepository _repository;

    public GetAllEvents(ICalendarEventRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetAllEventsResponse>> HandleAsync(GetAllEventsRequest request, CancellationToken cancellationToken)
    {
        var events = _repository.FindAll().Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetAllEventsResponse>>(new GetAllEventsResponse(events));
    }
}
