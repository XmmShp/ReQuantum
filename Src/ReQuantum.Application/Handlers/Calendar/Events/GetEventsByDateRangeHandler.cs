using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar.Events;

/// <summary>
/// 按日期范围获取日程事件
/// </summary>
public class GetEventsByDateRange : IRequestHandler<GetEventsByDateRangeRequest, GetEventsByDateRangeResponse>
{
    private readonly ICalendarEventRepository _repository;

    public GetEventsByDateRange(ICalendarEventRepository repository)
    {
        _repository = repository;
    }

    public Task<Result<GetEventsByDateRangeResponse>> HandleAsync(GetEventsByDateRangeRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return Task.FromResult<Result<GetEventsByDateRangeResponse>>(
                Result.Fail(CalendarFailures.InvalidDateRange));
        }

        var events = _repository.FindByDateRange(request.StartDate, request.EndDate).Select(CalendarMapper.ToContract).ToList();
        return Task.FromResult<Result<GetEventsByDateRangeResponse>>(new GetEventsByDateRangeResponse(events));
    }
}
