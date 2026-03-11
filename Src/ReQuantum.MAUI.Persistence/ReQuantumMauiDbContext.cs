using Microsoft.EntityFrameworkCore;
using NOF.Infrastructure.EntityFrameworkCore;
using ReQuantum.Domain.Calendar;
using System.Text.Json;

namespace ReQuantum.MAUI.Persistence;

public class ReQuantumMauiDbContext : NOFDbContext
{
    public DbSet<CalendarEvent> CalendarEvents { get; set; }
    public DbSet<CalendarTodo> CalendarTodos { get; set; }
    public DbSet<CalendarNote> CalendarNotes { get; set; }
    public DbSet<StorageEntry> StorageEntries { get; set; }

    public ReQuantumMauiDbContext(DbContextOptions<ReQuantumMauiDbContext> options)
        : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CalendarTodo>(entity =>
        {
            entity.Property(e => e.Properties)
                .HasConversion(
                    value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                    value => string.IsNullOrWhiteSpace(value)
                        ? new Dictionary<string, object?>()
                        : JsonSerializer.Deserialize<Dictionary<string, object?>>(value, (JsonSerializerOptions?)null)
                            ?? new Dictionary<string, object?>());
        });
    }
}
