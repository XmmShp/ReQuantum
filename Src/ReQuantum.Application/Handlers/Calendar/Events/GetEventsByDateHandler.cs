using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Events;

/// <summary>
/// 按日期获取日程事件
/// </summary>
public class GetEventsByDate : IRequestHandler<GetEventsByDateRequest, GetEventsByDateResponse>
{
    private readonly ICalendarEventRepository _repository;

    public GetEventsByDate(ICalendarEventRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetEventsByDateResponse>> HandleAsync(GetEventsByDateRequest request, CancellationToken cancellationToken)
    {
        var events = _repository.FindByDate(request.Date).Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetEventsByDateResponse>>(new GetEventsByDateResponse(events));
    }
}
