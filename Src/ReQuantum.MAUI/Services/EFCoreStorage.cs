using Microsoft.EntityFrameworkCore;
using ReQuantum.MAUI.Persistence;
using ReQuantum.Shared.Services;

namespace ReQuantum.Infrastructure.Services;

public class EFCoreStorage : IStorage
{
    private readonly ReQuantumMauiDbContext _dbContext;

    public EFCoreStorage(ReQuantumMauiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<bool> ContainsAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _dbContext.StorageEntries.AnyAsync(e => e.Key == key, cancellationToken: cancellationToken);
    }

    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.StorageEntries.FindAsync([key], cancellationToken);
        if (existing is null)
        {
            _dbContext.StorageEntries.Add(new StorageEntry { Key = key, Value = value });
            return;
        }

        existing.Value = value;
    }

    public async ValueTask<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.StorageEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Key == key, cancellationToken: cancellationToken);
        return entry?.Value ?? throw new KeyNotFoundException($"Key '{key}' not found");
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = await _dbContext.StorageEntries.FindAsync([key], cancellationToken);
        if (entry is not null)
        {
            _dbContext.StorageEntries.Remove(entry);
        }
    }
}
