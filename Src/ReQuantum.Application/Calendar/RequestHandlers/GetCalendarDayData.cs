using NOF.Application;
using NOF.Contract;
using ReQuantum.Application.Calendar.Abstractions;
using ReQuantum.Contract.Calendar;
using ReQuantum.Domain.Calendar.Repositories;
using CalendarEvent = ReQuantum.Domain.Calendar.AggregateRoots.CalendarEvent;
using CalendarEventDto = ReQuantum.Contract.Calendar.CalendarEvent;
using CalendarNote = ReQuantum.Domain.Calendar.AggregateRoots.CalendarNote;
using CalendarNoteDto = ReQuantum.Contract.Calendar.CalendarNote;
using CalendarTodo = ReQuantum.Domain.Calendar.AggregateRoots.CalendarTodo;
using CalendarTodoDto = ReQuantum.Contract.Calendar.CalendarTodo;

namespace ReQuantum.Application.Calendar.RequestHandlers;

/// <summary>
/// 获取指定日期的日历综合数据
/// </summary>
public class GetCalendarDayData(
    ICalendarEventRepository eventRepository,
    ICalendarTodoRepository todoRepository,
    ICalendarNoteRepository noteRepository,
    IEnumerable<ICalendarTodoProvider> todoProviders)
    : IRequestHandler<GetCalendarDayDataRequest, GetCalendarDayDataResponse>
{
    public async Task<Result<GetCalendarDayDataResponse>> HandleAsync(GetCalendarDayDataRequest request, CancellationToken cancellationToken)
    {
        var events = new List<CalendarEvent>();
        await foreach (var calendarEvent in eventRepository.FindByDateAsync(request.Date, cancellationToken))
        {
            events.Add(calendarEvent);
        }

        var todos = new List<CalendarTodo>();
        await foreach (var todo in todoRepository.FindByDateAsync(request.Date, cancellationToken))
        {
            todos.Add(todo);
        }

        var notes = new List<CalendarNote>();
        await foreach (var note in noteRepository.FindAllByCreatedAtDescendingAsync(cancellationToken))
        {
            notes.Add(note);
        }

        var todoList = todos.Select(t => t.Map.To<CalendarTodoDto>()).ToList();
        foreach (var provider in todoProviders)
        {
            await foreach (var todo in provider.GetTodosAsync(request.Date, request.Date, cancellationToken))
            {
                todoList.Add(todo);
            }
        }

        var data = new CalendarDayData(
            request.Date,
            todoList,
            events.Select(e => e.Map.To<CalendarEventDto>()).ToList(),
            notes.Select(n => n.Map.To<CalendarNoteDto>()).ToList());

        return new GetCalendarDayDataResponse(data);
    }
}
