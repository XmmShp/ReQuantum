using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar.Events;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarEventDto = ReQuantum.Contract.Calendar.CalendarEvent;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 获取所有日程事件
/// </summary>
public class GetAllEvents(ICalendarEventRepository repository) : IRequestHandler<GetAllEventsRequest, GetAllEventsResponse>
{
    public async Task<Result<GetAllEventsResponse>> HandleAsync(GetAllEventsRequest request, CancellationToken cancellationToken)
    {
        var events = new List<CalendarEventDto>();
        await foreach (var calendarEvent in repository.FindAllAsync(cancellationToken))
        {
            events.Add(calendarEvent.Map.To<CalendarEventDto>());
        }

        return new GetAllEventsResponse(events);
    }
}
