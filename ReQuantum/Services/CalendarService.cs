using ReQuantum.Attributes;
using ReQuantum.Extensions;
using ReQuantum.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ReQuantum.Services;

public interface ICalendarService
{
    // 便签相关
    List<CalendarNote> GetAllNotes();
    void AddNote(CalendarNote note);
    void UpdateNote(CalendarNote note);
    void DeleteNote(Guid id);

    // 待办相关
    List<CalendarTodo> GetAllTodos();
    List<CalendarTodo> GetTodosByDate(DateOnly date); // 获取指定日期截止的所有待办（无论是否完成）
    List<CalendarTodo> GetTodosByDateRange(DateOnly startDate, DateOnly endDate);
    List<CalendarTodo> GetIncompleteTodosByDate(DateOnly date);
    void AddTodo(CalendarTodo todo);
    void UpdateTodo(CalendarTodo todo);
    void DeleteTodo(Guid id);
    void ToggleTodoComplete(Guid id);

    // 日程相关
    List<CalendarEvent> GetAllEvents();
    List<CalendarEvent> GetEventsByDate(DateOnly date);
    List<CalendarEvent> GetEventsByDateRange(DateOnly startDate, DateOnly endDate);
    void AddEvent(CalendarEvent calendarEvent);
    void UpdateEvent(CalendarEvent calendarEvent);
    void DeleteEvent(Guid id);
}

[AutoInject(Lifetime.Singleton)]
public class CalendarService : ICalendarService
{
    private readonly IStorage _storage;
    private const string NotesKey = "Calendar:Notes";
    private const string TodosKey = "Calendar:Todos";
    private const string EventsKey = "Calendar:Events";

    private List<CalendarNote> _notes;
    private List<CalendarTodo> _todos;
    private List<CalendarEvent> _events;

    public CalendarService(IStorage storage)
    {
        _storage = storage;
        LoadData();
    }

    #region 便签管理

    public List<CalendarNote> GetAllNotes()
    {
        return _notes.ToList();
    }

    public void AddNote(CalendarNote note)
    {
        _notes.Add(note);
        SaveNotes();
    }

    public void UpdateNote(CalendarNote note)
    {
        var index = _notes.FindIndex(n => n.Id == note.Id);
        if (index >= 0)
        {
            _notes[index] = note;
            SaveNotes();
        }
    }

    public void DeleteNote(Guid id)
    {
        _notes.RemoveAll(n => n.Id == id);
        SaveNotes();
    }

    #endregion

    #region 待办管理

    public List<CalendarTodo> GetAllTodos()
    {
        return _todos.ToList();
    }

    public List<CalendarTodo> GetTodosByDate(DateOnly date)
    {
        return _todos
            .Where(t => DateOnly.FromDateTime(t.DueTime) == date)
            .OrderBy(t => t.DueTime)
            .ToList();
    }

    public List<CalendarTodo> GetTodosByDateRange(DateOnly startDate, DateOnly endDate)
    {
        return _todos
            .Where(t => DateOnly.FromDateTime(t.DueTime) >= startDate &&
                       DateOnly.FromDateTime(t.DueTime) <= endDate)
            .OrderBy(t => t.DueTime)
            .ToList();
    }

    public List<CalendarTodo> GetIncompleteTodosByDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);

        return _todos
            .Where(t => !t.IsCompleted &&
                       DateOnly.FromDateTime(t.DueTime) >= today && // 截止日期在今天或之后
                       DateOnly.FromDateTime(t.DueTime) >= date)    // 截止日期在选中日期或之后
            .OrderBy(t => t.DueTime)
            .ToList();
    }

    public void AddTodo(CalendarTodo todo)
    {
        _todos.Add(todo);
        SaveTodos();
    }

    public void UpdateTodo(CalendarTodo todo)
    {
        var index = _todos.FindIndex(t => t.Id == todo.Id);
        if (index >= 0)
        {
            _todos[index] = todo;
            SaveTodos();
        }
    }

    public void DeleteTodo(Guid id)
    {
        _todos.RemoveAll(t => t.Id == id);
        SaveTodos();
    }

    public void ToggleTodoComplete(Guid id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is not null)
        {
            todo.IsCompleted = !todo.IsCompleted;
            SaveTodos();
        }
    }

    #endregion

    #region 日程管理

    public List<CalendarEvent> GetAllEvents()
    {
        return _events.ToList();
    }

    public List<CalendarEvent> GetEventsByDate(DateOnly date)
    {
        return _events
            .Where(e => DateOnly.FromDateTime(e.StartTime) == date)
            .OrderBy(e => e.StartTime)
            .ToList();
    }

    public List<CalendarEvent> GetEventsByDateRange(DateOnly startDate, DateOnly endDate)
    {
        return _events
            .Where(e =>
            {
                var eventDate = DateOnly.FromDateTime(e.StartTime);
                return eventDate >= startDate && eventDate <= endDate;
            })
            .OrderBy(e => e.StartTime)
            .ToList();
    }

    public void AddEvent(CalendarEvent calendarEvent)
    {
        _events.Add(calendarEvent);
        SaveEvents();
    }

    public void UpdateEvent(CalendarEvent calendarEvent)
    {
        var index = _events.FindIndex(e => e.Id == calendarEvent.Id);
        if (index >= 0)
        {
            _events[index] = calendarEvent;
            SaveEvents();
        }
    }

    public void DeleteEvent(Guid id)
    {
        _events.RemoveAll(e => e.Id == id);
        SaveEvents();
    }

    #endregion

    #region 数据持久化

    [MemberNotNull(nameof(_notes))]
    [MemberNotNull(nameof(_todos))]
    [MemberNotNull(nameof(_events))]
    private void LoadData()
    {
        _notes = _storage.TryGet<List<CalendarNote>>(NotesKey, out var notes) && notes is not null
            ? notes
            : [];

        _todos = _storage.TryGet<List<CalendarTodo>>(TodosKey, out var todos) && todos is not null
            ? todos
            : [];

        _events = _storage.TryGet<List<CalendarEvent>>(EventsKey, out var events) && events is not null
            ? events
            : [];
    }

    private void SaveNotes()
    {
        _storage.Set(NotesKey, _notes);
    }

    private void SaveTodos()
    {
        _storage.Set(TodosKey, _todos);
    }

    private void SaveEvents()
    {
        _storage.Set(EventsKey, _events);
    }

    #endregion
}
