using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarEventDto = ReQuantum.Contract.Calendar.CalendarEvent;

namespace ReQuantum.Application.RequestHandlers.Calendar;

/// <summary>
/// 按日期获取日程事件
/// </summary>
public class GetEventsByDate(ICalendarEventRepository repository) : IRequestHandler<GetEventsByDateRequest, GetEventsByDateResponse>
{
    public async Task<Result<GetEventsByDateResponse>> HandleAsync(GetEventsByDateRequest request, CancellationToken cancellationToken)
    {
        var events = new List<CalendarEventDto>();
        await foreach (var calendarEvent in repository.FindByDateAsync(request.Date, cancellationToken))
        {
            events.Add(calendarEvent.Map.To<CalendarEventDto>());
        }

        return new GetEventsByDateResponse(events);
    }
}
