using NOF.Annotation;
using ReQuantum.Domain.Calendar;
using ReQuantum.Shared.Services;

namespace ReQuantum.Application.Repositories.Calendar;

/// <summary>
/// 基于 IStorage 的待办事项仓储实现
/// </summary>
[AutoInject(Lifetime.Singleton)]
public class StorageCalendarTodoRepository : ICalendarTodoRepository
{
    private readonly IStorage _storage;
    private const string StorageKey = "Calendar:Todos";
    private List<CalendarTodo> _todos;

    public StorageCalendarTodoRepository(IStorage storage)
    {
        _storage = storage;
        _todos = _storage.TryGet<List<CalendarTodo>>(StorageKey, out var todos) && todos is not null
            ? todos
            : [];
    }

    public List<CalendarTodo> FindAll() => _todos.ToList();

    public List<CalendarTodo> FindByDate(DateOnly date) =>
        _todos
            .Where(t => DateOnly.FromDateTime(t.DueTime) == date)
            .OrderBy(t => t.DueTime)
            .ToList();

    public List<CalendarTodo> FindByDateRange(DateOnly startDate, DateOnly endDate) =>
        _todos
            .Where(t => DateOnly.FromDateTime(t.DueTime) >= startDate && DateOnly.FromDateTime(t.DueTime) <= endDate)
            .OrderBy(t => t.DueTime)
            .ToList();

    public List<CalendarTodo> FindIncompleteByDate(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        return _todos
            .Where(t => !t.IsCompleted && DateOnly.FromDateTime(t.DueTime) >= today && DateOnly.FromDateTime(t.DueTime) >= date)
            .OrderBy(t => t.DueTime)
            .ToList();
    }

    public CalendarTodo? FindById(CalendarTodoId id) =>
        _todos.FirstOrDefault(t => t.Id == id);

    public void AddOrUpdate(CalendarTodo todo)
    {
        var index = _todos.FindIndex(t => t.Id == todo.Id);
        if (index >= 0)
        {
            _todos[index] = todo;
        }
        else
        {
            _todos.Add(todo);
        }

        Save();
    }

    public void Delete(CalendarTodoId id)
    {
        _todos.RemoveAll(t => t.Id == id);
        Save();
    }

    private void Save() => _storage.Set(StorageKey, _todos);
}
