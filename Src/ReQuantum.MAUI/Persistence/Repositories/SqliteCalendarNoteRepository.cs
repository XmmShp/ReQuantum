using Microsoft.EntityFrameworkCore;
using ReQuantum.Domain.Calendar;
using ReQuantum.MAUI.Persistence;

namespace ReQuantum.Infrastructure.Persistence.Repositories;

public class SqliteCalendarNoteRepository : ICalendarNoteRepository
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqliteCalendarNoteRepository(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public List<CalendarNote> FindAll()
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarNotes.AsNoTracking().OrderByDescending(n => n.CreatedAt).ToList();
    }

    public CalendarNote? FindById(CalendarNoteId id)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.CalendarNotes.AsNoTracking().FirstOrDefault(n => n.Id == id);
    }

    public void AddOrUpdate(CalendarNote note)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var existing = dbContext.CalendarNotes.FirstOrDefault(n => n.Id == note.Id);

        if (existing is null)
        {
            dbContext.CalendarNotes.Add(note);
        }
        else
        {
            dbContext.Entry(existing).CurrentValues.SetValues(note);
        }

        dbContext.SaveChanges();
    }

    public void Delete(CalendarNoteId id)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var existing = dbContext.CalendarNotes.FirstOrDefault(n => n.Id == id);
        if (existing is null)
        {
            return;
        }

        dbContext.CalendarNotes.Remove(existing);
        dbContext.SaveChanges();
    }
}
