using NOF.Annotation;
using ReQuantum.Application.Handlers.Calendar;
using ContractCalendarEvent = ReQuantum.Contract.Calendar.CalendarEvent;
using ContractCalendarTodo = ReQuantum.Contract.Calendar.CalendarTodo;
using ContractCalendarNote = ReQuantum.Contract.Calendar.CalendarNote;
using ContractCalendarDayData = ReQuantum.Contract.Calendar.CalendarDayData;
using ReQuantum.Domain.Calendar;

namespace ReQuantum.Application.Services.Calendar;

/// <summary>
/// 日历服务 - 委托给领域层仓储实现，并通过 Contract 类型暴露给 UI 层
/// </summary>
[AutoInject(Lifetime.Singleton)]
public class CalendarService : ICalendarService
{
    private readonly ICalendarEventRepository _eventRepository;
    private readonly ICalendarTodoRepository _todoRepository;
    private readonly ICalendarNoteRepository _noteRepository;

    public CalendarService(
        ICalendarEventRepository eventRepository,
        ICalendarTodoRepository todoRepository,
        ICalendarNoteRepository noteRepository)
    {
        _eventRepository = eventRepository;
        _todoRepository = todoRepository;
        _noteRepository = noteRepository;
    }

    #region 便签管理

    public List<ContractCalendarNote> FindAllNotes() =>
        _noteRepository.FindAll().Select(CalendarMapper.ToContract).ToList();

    public void AddOrUpdateNote(ContractCalendarNote note)
    {
        if (note.Id != 0)
        {
            var existing = _noteRepository.FindById(CalendarNoteId.Of(note.Id));
            if (existing is not null)
            {
                existing.Update(note.Content);
                _noteRepository.AddOrUpdate(existing);
                return;
            }
        }

        var entity = CalendarNote.Create(note.Content);
        _noteRepository.AddOrUpdate(entity);
    }

    public void DeleteNote(long id) =>
        _noteRepository.Delete(CalendarNoteId.Of(id));

    #endregion

    #region 待办管理

    public List<ContractCalendarTodo> FindAllTodos() =>
        _todoRepository.FindAll().Select(CalendarMapper.ToContract).ToList();

    public List<ContractCalendarTodo> FindTodosByDate(DateOnly date) =>
        _todoRepository.FindByDate(date).Select(CalendarMapper.ToContract).ToList();

    public List<ContractCalendarTodo> FindTodosByDateRange(DateOnly startDate, DateOnly endDate) =>
        _todoRepository.FindByDateRange(startDate, endDate).Select(CalendarMapper.ToContract).ToList();

    public List<ContractCalendarTodo> FindIncompleteTodosByDate(DateOnly date) =>
        _todoRepository.FindIncompleteByDate(date).Select(CalendarMapper.ToContract).ToList();

    public void AddOrUpdateTodo(ContractCalendarTodo todo)
    {
        if (todo.Id != 0)
        {
            var existing = _todoRepository.FindById(CalendarTodoId.Of(todo.Id));
            if (existing is not null)
            {
                existing.Update(todo.Content, todo.DueTime);
                if (todo.IsCompleted != existing.IsCompleted)
                {
                    existing.ToggleComplete();
                }

                _todoRepository.AddOrUpdate(existing);
                return;
            }
        }

        var entity = CalendarTodo.Create(todo.Content, todo.DueTime);
        _todoRepository.AddOrUpdate(entity);
    }

    public void DeleteTodo(long id) =>
        _todoRepository.Delete(CalendarTodoId.Of(id));

    public void ToggleTodoComplete(long id)
    {
        var todo = _todoRepository.FindById(CalendarTodoId.Of(id));
        if (todo is not null)
        {
            todo.ToggleComplete();
            _todoRepository.AddOrUpdate(todo);
        }
    }

    #endregion

    #region 日程管理

    public List<ContractCalendarEvent> FindAllEvents() =>
        _eventRepository.FindAll().Select(CalendarMapper.ToContract).ToList();

    public List<ContractCalendarEvent> FindEventsByDate(DateOnly date) =>
        _eventRepository.FindByDate(date).Select(CalendarMapper.ToContract).ToList();

    public List<ContractCalendarEvent> FindEventsByDateRange(DateOnly startDate, DateOnly endDate) =>
        _eventRepository.FindByDateRange(startDate, endDate).Select(CalendarMapper.ToContract).ToList();

    public void AddOrUpdateEvent(ContractCalendarEvent calendarEvent)
    {
        if (calendarEvent.Id != 0)
        {
            var existing = _eventRepository.FindById(CalendarEventId.Of(calendarEvent.Id));
            if (existing is not null)
            {
                existing.Update(calendarEvent.Content, calendarEvent.StartTime, calendarEvent.EndTime, calendarEvent.Note);
                _eventRepository.AddOrUpdate(existing);
                return;
            }
        }

        var source = (CalendarEventSource)calendarEvent.Source;
        var entity = source == CalendarEventSource.Manual
            ? CalendarEvent.Create(calendarEvent.Content, calendarEvent.StartTime, calendarEvent.EndTime, calendarEvent.Note)
            : CalendarEvent.CreateFromSource(
                calendarEvent.Content,
                calendarEvent.StartTime,
                calendarEvent.EndTime,
                source,
                calendarEvent.From);

        _eventRepository.AddOrUpdate(entity);
    }

    public void DeleteEvent(long id) =>
        _eventRepository.Delete(CalendarEventId.Of(id));

    #endregion

    #region 日历数据生成

    public ContractCalendarDayData FindCalendarDayData(DateOnly date)
    {
        var events = _eventRepository.FindByDate(date);
        var todos = _todoRepository.FindByDate(date);
        var notes = _noteRepository.FindAll();

        return new ContractCalendarDayData(
            date,
            todos.Select(CalendarMapper.ToContract).ToList(),
            events.Select(CalendarMapper.ToContract).ToList(),
            notes.Select(CalendarMapper.ToContract).ToList());
    }

    #endregion
}
