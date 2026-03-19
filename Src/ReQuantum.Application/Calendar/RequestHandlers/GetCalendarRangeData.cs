using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarEvent = ReQuantum.Domain.Calendar.AggregateRoots.CalendarEvent;
using CalendarEventDto = ReQuantum.Contract.Calendar.CalendarEvent;
using CalendarTodo = ReQuantum.Domain.Calendar.AggregateRoots.CalendarTodo;
using CalendarTodoDto = ReQuantum.Contract.Calendar.CalendarTodo;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 按日期范围获取日历综合数据。
/// </summary>
public class GetCalendarRangeData(
    ICalendarEventRepository eventRepository,
    ICalendarTodoRepository todoRepository)
    : IRequestHandler<GetCalendarRangeDataRequest, GetCalendarRangeDataResponse>
{
    public async Task<Result<GetCalendarRangeDataResponse>> HandleAsync(GetCalendarRangeDataRequest request, CancellationToken cancellationToken)
    {
        if (request.StartDate > request.EndDate)
        {
            return Result.Fail(CalendarFailures.InvalidDateRange);
        }

        var events = new List<CalendarEvent>();
        await foreach (var calendarEvent in eventRepository.FindByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken))
        {
            events.Add(calendarEvent);
        }

        var todos = new List<CalendarTodo>();
        await foreach (var todo in todoRepository.FindByDateRangeAsync(request.StartDate, request.EndDate, cancellationToken))
        {
            todos.Add(todo);
        }

        var dataByDate = new Dictionary<DateOnly, CalendarDayData>();
        for (var day = request.StartDate; day <= request.EndDate; day = day.AddDays(1))
        {
            dataByDate[day] = new CalendarDayData(day, [], [], []);
        }

        foreach (var calendarEvent in events)
        {
            var eventDto = calendarEvent.Map.To<CalendarEventDto>();
            var eventStartDate = DateOnly.FromDateTime(calendarEvent.StartTime);
            var eventEndDate = DateOnly.FromDateTime(calendarEvent.EndTime);
            var overlapStart = eventStartDate > request.StartDate ? eventStartDate : request.StartDate;
            var overlapEnd = eventEndDate < request.EndDate ? eventEndDate : request.EndDate;

            for (var day = overlapStart; day <= overlapEnd; day = day.AddDays(1))
            {
                if (dataByDate.TryGetValue(day, out var dayData))
                {
                    dayData.Events.Add(eventDto);
                }
            }
        }

        foreach (var todo in todos)
        {
            var todoDate = DateOnly.FromDateTime(todo.DueTime);
            if (!dataByDate.TryGetValue(todoDate, out var dayData))
            {
                continue;
            }

            dayData.Todos.Add(todo.Map.To<CalendarTodoDto>());
        }

        return new GetCalendarRangeDataResponse(dataByDate);
    }
}
