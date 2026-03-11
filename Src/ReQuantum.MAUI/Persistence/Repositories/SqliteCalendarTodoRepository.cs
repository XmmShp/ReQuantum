using Microsoft.EntityFrameworkCore;
using ReQuantum.Domain.Calendar;
using ReQuantum.MAUI.Persistence;

namespace ReQuantum.Infrastructure.Persistence.Repositories;

public class SqliteCalendarTodoRepository : ICalendarTodoRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqliteCalendarTodoRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public List<CalendarTodo> FindAll()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarTodos.AsNoTracking().OrderBy(t => t.DueTime).ToList();
    }

    public List<CalendarTodo> FindByDate(DateOnly date)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarTodos
            .AsNoTracking()
            .OrderBy(t => t.DueTime)
            .ToList()
            .Where(t => DateOnly.FromDateTime(t.DueTime) == date)
            .ToList();
    }

    public List<CalendarTodo> FindByDateRange(DateOnly startDate, DateOnly endDate)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarTodos
            .AsNoTracking()
            .OrderBy(t => t.DueTime)
            .ToList()
            .Where(t => DateOnly.FromDateTime(t.DueTime) >= startDate && DateOnly.FromDateTime(t.DueTime) <= endDate)
            .ToList();
    }

    public List<CalendarTodo> FindIncompleteByDate(DateOnly date)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var today = DateOnly.FromDateTime(DateTime.Now);
        return dbContext.CalendarTodos
            .AsNoTracking()
            .OrderBy(t => t.DueTime)
            .ToList()
            .Where(t => !t.IsCompleted && DateOnly.FromDateTime(t.DueTime) >= today && DateOnly.FromDateTime(t.DueTime) >= date)
            .ToList();
    }

    public CalendarTodo? FindById(CalendarTodoId id)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarTodos.AsNoTracking().FirstOrDefault(t => t.Id == id);
    }

    public void AddOrUpdate(CalendarTodo todo)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var existing = dbContext.CalendarTodos.FirstOrDefault(t => t.Id == todo.Id);

        if (existing is null)
        {
            dbContext.CalendarTodos.Add(todo);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(todo);
        }

        dbContext.SaveChanges();
    }

    public void Delete(CalendarTodoId id)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var existing = dbContext.CalendarTodos.FirstOrDefault(t => t.Id == id);
        if (existing is null)
        {
            return;
        }

        dbContext.CalendarTodos.Remove(existing);
        dbContext.SaveChanges();
    }
}
