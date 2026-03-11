using Microsoft.EntityFrameworkCore;
using ReQuantum.MAUI.Persistence;
using ReQuantum.Shared.Services;

namespace ReQuantum.Infrastructure.Services;

public class SqliteStorage : IStorage
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SqliteStorage(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public bool ContainsKey(string key)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return dbContext.StorageEntries.Any(e => e.Id == key);
    }

    public void SetString(string key, string value)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        SaveStorageEntry(dbContext, key, value);
        dbContext.SaveChanges();
    }

    public string GetString(string key)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return LoadStorageEntry(dbContext, key);
    }

    public void Remove(string key)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var entry = dbContext.StorageEntries.FirstOrDefault(e => e.Id == key);
        if (entry is not null)
        {
            dbContext.StorageEntries.Remove(entry);
        }

        dbContext.SaveChanges();
    }

    private static void SaveStorageEntry(ReQuantumMauiDbContext dbContext, string key, string value)
    {
        var existing = dbContext.StorageEntries.FirstOrDefault(e => e.Id == key);
        if (existing is null)
        {
            dbContext.StorageEntries.Add(new StorageEntry { Id = key, Value = value });
            return;
        }

        existing.Value = value;
    }

    private static string LoadStorageEntry(ReQuantumMauiDbContext dbContext, string key)
    {
        var entry = dbContext.StorageEntries.AsNoTracking().FirstOrDefault(e => e.Id == key);
        return entry?.Value ?? throw new KeyNotFoundException($"Key '{key}' not found");
    }
}
