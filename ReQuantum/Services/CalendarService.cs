using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Extensions;
using ReQuantum.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace ReQuantum.Services;

public interface ICalendarService
{
    // 便签相关
    List<CalendarNote> GetAllNotes();
    void AddOrUpdateNote(CalendarNote note);
    void DeleteNote(Guid id);

    // 待办相关
    List<CalendarTodo> GetAllTodos();
    List<CalendarTodo> GetTodosByDate(DateOnly date); // 获取指定日期截止的所有待办（无论是否完成）
    List<CalendarTodo> GetTodosByDateRange(DateOnly startDate, DateOnly endDate);
    List<CalendarTodo> GetIncompleteTodosByDate(DateOnly date);
    void AddOrUpdateTodo(CalendarTodo todo);
    void DeleteTodo(Guid id);
    void ToggleTodoComplete(Guid id);

    // 日程相关
    List<CalendarEvent> GetAllEvents();
    List<CalendarEvent> GetEventsByDate(DateOnly date);
    List<CalendarEvent> GetEventsByDateRange(DateOnly startDate, DateOnly endDate);
    void AddOrUpdateEvent(CalendarEvent calendarEvent);
    void DeleteEvent(Guid id);

    // 日历生成相关
    CalendarDayData GetCalendarDayData(DateOnly date);
    List<CalendarDayData> GetMonthCalendarData(int year, int month);
    List<CalendarDayData> GetWeekCalendarData(DateOnly weekStartDate);
}

[AutoInject(Lifetime.Singleton)]
public class CalendarService : ICalendarService, IInitializable
{
    private readonly IStorage _storage;
    private const string NotesKey = "Calendar:Notes";
    private const string TodosKey = "Calendar:Todos";
    private const string EventsKey = "Calendar:Events";

    private List<CalendarNote> _notes = [];
    private List<CalendarTodo> _todos = [];
    private List<CalendarEvent> _events = [];

    // 日历数据字典 - 全局唯一，每个日期只有一个对象
    private readonly ConcurrentDictionary<DateOnly, CalendarDayData> _calendarDataDict = [];

    public CalendarService(IStorage storage)
    {
        _storage = storage;
    }

    #region 便签管理

    public List<CalendarNote> GetAllNotes()
    {
        return _notes.ToList();
    }

    public void AddOrUpdateNote(CalendarNote note)
    {
        var index = _notes.FindIndex(n => n.Id == note.Id);
        if (index >= 0)
        {
            _notes[index] = note;
        }
        else
        {
            _notes.Add(note);
        }

        SaveNotes();
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

    public void AddOrUpdateTodo(CalendarTodo todo)
    {
        var date = DateOnly.FromDateTime(todo.DueTime);
        var index = _todos.FindIndex(t => t.Id == todo.Id);

        if (index >= 0)
        {
            _todos[index] = todo;

            // 更新已存在日期的集合中的对应项
            if (_calendarDataDict.TryGetValue(date, out var dayData))
            {
                var existingIndex = dayData.Todos.ToList().FindIndex(t => t.Id == todo.Id);
                if (existingIndex >= 0)
                {
                    dayData.Todos[existingIndex] = todo;
                }
            }
        }
        else
        {
            _todos.Add(todo);

            // 添加到已存在日期的集合中
            if (_calendarDataDict.TryGetValue(date, out var dayData))
            {
                dayData.Todos.Add(todo);
            }
        }

        SaveTodos();
    }

    public void DeleteTodo(Guid id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is null)
        {
            return;
        }

        var date = DateOnly.FromDateTime(todo.DueTime);
        _todos.Remove(todo);

        if (_calendarDataDict.TryGetValue(date, out var dayData))
        {
            dayData.Todos.Remove(todo);
        }

        SaveTodos();
    }

    public void ToggleTodoComplete(Guid id)
    {
        var todo = _todos.FirstOrDefault(t => t.Id == id);
        if (todo is null)
        {
            return;
        }

        todo.IsCompleted = !todo.IsCompleted;

        SaveTodos();
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

    public void AddOrUpdateEvent(CalendarEvent calendarEvent)
    {
        var startDate = DateOnly.FromDateTime(calendarEvent.StartTime);
        var endDate = DateOnly.FromDateTime(calendarEvent.EndTime);
        var index = _events.FindIndex(e => e.Id == calendarEvent.Id);

        if (index >= 0)
        {
            var oldEvent = _events[index];
            var oldStartDate = DateOnly.FromDateTime(oldEvent.StartTime);
            var oldEndDate = DateOnly.FromDateTime(oldEvent.EndTime);

            _events[index] = calendarEvent;

            // 从旧日期范围中移除
            for (var date = oldStartDate; date <= oldEndDate; date = date.AddDays(1))
            {
                if (_calendarDataDict.TryGetValue(date, out var dayData))
                {
                    dayData.Events.Remove(oldEvent);
                }
            }

            // 添加到新日期范围
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (_calendarDataDict.TryGetValue(date, out var dayData))
                {
                    dayData.Events.Add(calendarEvent);
                }
            }
        }
        else
        {
            _events.Add(calendarEvent);

            // 添加到所有相关日期（仅已存在的）
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                if (_calendarDataDict.TryGetValue(date, out var dayData))
                {
                    dayData.Events.Add(calendarEvent);
                }
            }
        }

        SaveEvents();
    }

    public void DeleteEvent(Guid id)
    {
        var evt = _events.FirstOrDefault(e => e.Id == id);
        if (evt is null)
        {
            return;
        }
        var startDate = DateOnly.FromDateTime(evt.StartTime);
        var endDate = DateOnly.FromDateTime(evt.EndTime);

        _events.Remove(evt);

        // 从所有相关日期中删除（仅已存在的）
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (_calendarDataDict.TryGetValue(date, out var dayData))
            {
                dayData.Events.Remove(evt);
            }
        }

        SaveEvents();
    }

    #endregion

    #region 数据持久化

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

    #region 日历数据生成

    /// <summary>
    /// 获取指定日期的日历数据（确保全局唯一）
    /// </summary>
    public CalendarDayData GetCalendarDayData(DateOnly date)
    {
        // 如果已存在，直接返回
        if (_calendarDataDict.TryGetValue(date, out var existingData))
        {
            return existingData;
        }

        // 创建新的数据对象
        var dayTodos = _todos.Where(t => DateOnly.FromDateTime(t.DueTime) == date).ToList();
        var dayEvents = _events.Where(e =>
            DateOnly.FromDateTime(e.StartTime) <= date &&
            DateOnly.FromDateTime(e.EndTime) >= date).ToList();

        var dayData = new CalendarDayData
        {
            Date = date,
            Todos = new ObservableCollection<CalendarTodo>(dayTodos),
            Events = new ObservableCollection<CalendarEvent>(dayEvents)
        };

        _calendarDataDict[date] = dayData;
        return dayData;
    }

    /// <summary>
    /// 获取月视图日历数据
    /// 只返回当月的日期数据，不包括前后填充
    /// </summary>
    public List<CalendarDayData> GetMonthCalendarData(int year, int month)
    {
        var result = new List<CalendarDayData>();
        var daysInMonth = DateTime.DaysInMonth(year, month);

        // 只返回当月的日期
        for (var day = 1; day <= daysInMonth; day++)
        {
            var date = new DateOnly(year, month, day);
            result.Add(GetCalendarDayData(date));
        }

        return result;
    }

    /// <summary>
    /// 获取周视图日历数据
    /// 返回该周7天的数据
    /// </summary>
    public List<CalendarDayData> GetWeekCalendarData(DateOnly weekStartDate)
    {
        var result = new List<CalendarDayData>();

        for (var i = 0; i < 7; i++)
        {
            var date = weekStartDate.AddDays(i);
            result.Add(GetCalendarDayData(date));
        }

        return result;
    }

    #endregion

    public Task InitializeAsync(IServiceProvider serviceProvider)
    {
        LoadData();
        return Task.CompletedTask;
    }
}
