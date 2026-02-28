using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using ReQuantum.Application.Models.Calendar;
using ReQuantum.Shared.Services;

namespace ReQuantum.Application.Services.Calendar;

public class CalendarService : ICalendarService
{
    private readonly IStorage _storage;
    private readonly ILogger<CalendarService> _logger;
    private const string NotesKey = "Calendar:Notes";
    private const string TodosKey = "Calendar:Todos";
    private const string EventsKey = "Calendar:Events";

    private List<CalendarNote> _notes = [];
    private List<CalendarTodo> _todos = [];
    private List<CalendarEvent> _events = [];

    private readonly ConcurrentDictionary<DateOnly, CalendarDayData> _calendarDataDict = [];

    public CalendarService(IStorage storage, ILogger<CalendarService> logger)
    {
        _storage = storage;
        _logger = logger;
        LoadData();
    }

    #region 便签管理

    public List<CalendarNote> GetAllNotes() => _notes.ToList();

    public void AddOrUpdateNote(CalendarNote note)
    {
        var index = _notes.FindIndex(n => n.Id == note.Id);
        if (index >= 0) _notes[index] = note;
        else _notes.Add(note);
        SaveNotes();
    }

    public void DeleteNote(Guid id)
    {
        _notes.RemoveAll(n => n.Id == id);
        SaveNotes();
    }

    #endregion

    #region 待办管理

    public List<CalendarTodo> GetAllTodos() => _todos.ToList();

    public List<CalendarTodo> GetTodosByDate(DateOnly date) =>
        _todos.Where(t => DateOnly.FromDateTime(t.DueTime) == date).OrderBy(t => t.DueTime).ToList();

    public List<CalendarTodo> GetTodosByDateRange(DateOnly startDate, DateOnly endDate) =>
        _todos.Where(t => DateOnly.FromDateTime(t.DueTime) >= startDate && DateOnly.FromDateTime(t.DueTime) <= endDate)
            .OrderBy(t => t.DueTime).ToList();

    public List<CalendarTodo> GetIncompleteTodosByDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return _todos.Where(t => !t.IsCompleted && DateOnly.FromDateTime(t.DueTime) >= today && DateOnly.FromDateTime(t.DueTime) >= date)
            .OrderBy(t => t.DueTime).ToList();
    }

    public void AddOrUpdateTodo(CalendarTodo todo)
    {
        var index = _todos.FindIndex(t => t.Id == todo.Id);
        if (index >= 0) _todos[index] = todo;
        else _todos.Add(todo);
        SaveTodos();
    }

    public void DeleteTodo(Guid id)
    {
        _todos.RemoveAll(t => t.Id == id);
        SaveTodos();
    }

    public void ToggleTodoComplete(Guid id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is not null) todo.IsCompleted = !todo.IsCompleted;
        SaveTodos();
    }

    #endregion

    #region 日程管理

    public List<CalendarEvent> GetAllEvents() => _events.ToList();

    public List<CalendarEvent> GetEventsByDate(DateOnly date) =>
        _events.Where(e => DateOnly.FromDateTime(e.StartTime) <= date && DateOnly.FromDateTime(e.EndTime) >= date)
            .OrderBy(e => e.StartTime).ToList();

    public List<CalendarEvent> GetEventsByDateRange(DateOnly startDate, DateOnly endDate) =>
        _events.Where(e => DateOnly.FromDateTime(e.StartTime) >= startDate && DateOnly.FromDateTime(e.StartTime) <= endDate)
            .OrderBy(e => e.StartTime).ToList();

    public void AddOrUpdateEvent(CalendarEvent calendarEvent)
    {
        var index = _events.FindIndex(e => e.Id == calendarEvent.Id);
        if (index >= 0) _events[index] = calendarEvent;
        else _events.Add(calendarEvent);
        SaveEvents();
    }

    public void DeleteEvent(Guid id)
    {
        _events.RemoveAll(e => e.Id == id);
        SaveEvents();
    }

    #endregion

    #region 日历数据生成

    public CalendarDayData GetCalendarDayData(DateOnly date)
    {
        if (_calendarDataDict.TryGetValue(date, out var existingData))
            return existingData;

        var dayData = new CalendarDayData
        {
            Date = date,
            Todos = _todos.Where(t => DateOnly.FromDateTime(t.DueTime) == date).ToList(),
            Events = _events.Where(e => DateOnly.FromDateTime(e.StartTime) <= date && DateOnly.FromDateTime(e.EndTime) >= date).ToList()
        };

        _calendarDataDict[date] = dayData;
        return dayData;
    }

    #endregion

    #region 数据持久化

    private void LoadData()
    {
        _notes = _storage.TryGet<List<CalendarNote>>(NotesKey, out var notes) && notes is not null ? notes : [];
        _todos = _storage.TryGet<List<CalendarTodo>>(TodosKey, out var todos) && todos is not null ? todos : [];
        _events = _storage.TryGet<List<CalendarEvent>>(EventsKey, out var events) && events is not null ? events : [];
    }

    private void SaveNotes() => _storage.Set(NotesKey, _notes);
    private void SaveTodos() => _storage.Set(TodosKey, _todos);
    private void SaveEvents() => _storage.Set(EventsKey, _events);

    #endregion
}
