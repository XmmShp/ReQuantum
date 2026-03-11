using Microsoft.EntityFrameworkCore;
using ReQuantum.Domain.Calendar;
using ReQuantum.MAUI.Persistence;

namespace ReQuantum.Infrastructure.Persistence.Repositories;

public class SqliteCalendarEventRepository : ICalendarEventRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqliteCalendarEventRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public List<CalendarEvent> FindAll()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarEvents.AsNoTracking().OrderBy(e => e.StartTime).ToList();
    }

    public List<CalendarEvent> FindByDate(DateOnly date)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarEvents
            .AsNoTracking()
            .OrderBy(e => e.StartTime)
            .ToList()
            .Where(e => DateOnly.FromDateTime(e.StartTime) <= date && DateOnly.FromDateTime(e.EndTime) >= date)
            .ToList();
    }

    public List<CalendarEvent> FindByDateRange(DateOnly startDate, DateOnly endDate)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarEvents
            .AsNoTracking()
            .OrderBy(e => e.StartTime)
            .ToList()
            .Where(e => DateOnly.FromDateTime(e.StartTime) >= startDate && DateOnly.FromDateTime(e.StartTime) <= endDate)
            .ToList();
    }

    public CalendarEvent? FindById(CalendarEventId id)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarEvents.AsNoTracking().FirstOrDefault(e => e.Id == id);
    }

    public void AddOrUpdate(CalendarEvent calendarEvent)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var existing = dbContext.CalendarEvents.FirstOrDefault(e => e.Id == calendarEvent.Id);

        if (existing is null)
        {
            dbContext.CalendarEvents.Add(calendarEvent);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(calendarEvent);
        }

        dbContext.SaveChanges();
    }

    public void Delete(CalendarEventId id)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var existing = dbContext.CalendarEvents.FirstOrDefault(e => e.Id == id);
        if (existing is null)
        {
            return;
        }

        dbContext.CalendarEvents.Remove(existing);
        dbContext.SaveChanges();
    }
}
