using NOF.Application;
using NOF.Contract;
using ReQuantum.Contract.Calendar;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Handlers.Calendar;

/// <summary>
/// 获取指定日期的日历综合数据
/// </summary>
public class GetCalendarDayData : IRequestHandler<GetCalendarDayDataRequest, GetCalendarDayDataResponse>
{
    private readonly ICalendarEventRepository _eventRepository;
    private readonly ICalendarTodoRepository _todoRepository;
    private readonly ICalendarNoteRepository _noteRepository;

    public GetCalendarDayData(
        ICalendarEventRepository eventRepository,
        ICalendarTodoRepository todoRepository,
        ICalendarNoteRepository noteRepository)
    {
        _eventRepository = eventRepository;
        _todoRepository = todoRepository;
        _noteRepository = noteRepository;
    }

    public Task<Result<GetCalendarDayDataResponse>> HandleAsync(GetCalendarDayDataRequest request, CancellationToken cancellationToken)
    {
        var events = _eventRepository.FindByDate(request.Date);
        var todos = _todoRepository.FindByDate(request.Date);
        var notes = _noteRepository.FindAll();

        var data = new CalendarDayData(
            request.Date,
            todos.Select(CalendarMapper.ToContract).ToList(),
            events.Select(CalendarMapper.ToContract).ToList(),
            notes.Select(CalendarMapper.ToContract).ToList());

        return Task.FromResult<Result<GetCalendarDayDataResponse>>(new GetCalendarDayDataResponse(data));
    }
}
