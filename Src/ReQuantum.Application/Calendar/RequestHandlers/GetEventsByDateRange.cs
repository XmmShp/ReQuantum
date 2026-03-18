using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarEventDto = ReQuantum.Contract.Calendar.CalendarEvent;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 按日期范围获取日程事件
/// </summary>
public class GetEventsByDateRange(ICalendarEventRepository repository) : IRequestHandler<GetEventsByDateRangeRequest, GetEventsByDateRangeResponse>
{
    public async Task<Result<GetEventsByDateRangeResponse>> HandleAsync(GetEventsByDateRangeRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return Result.Fail(CalendarFailures.InvalidDateRange);
        }

        var events = new List<CalendarEventDto>();
        await foreach (var calendarEvent in repository.FindByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken))
        {
            events.Add(calendarEvent.Map.To<CalendarEventDto>());
        }

        return new GetEventsByDateRangeResponse(events);
    }
}
