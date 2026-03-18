using Microsoft.EntityFrameworkCore;
using NOF.Annotation;
using ReQuantum.Application.Common.Services;
using ReQuantum.MAUI.Persistence;

namespace ReQuantum.Infrastructure.Services;

[AutoInject(Lifetime.Singleton)]
public class EFCoreStorage : IStorage
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public EFCoreStorage(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async ValueTask<bool> ContainsAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        return await dbContext.StorageEntries.AnyAsync(e => e.Key == key, cancellationToken: cancellationToken);
    }

    public async ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var existing = await dbContext.StorageEntries.FindAsync([key], cancellationToken);
        if (existing is null)
        {
            dbContext.StorageEntries.Add(new StorageEntry { Key = key, Value = value });
        }
        else
        {
            existing.Value = value;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async ValueTask<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var entry = await dbContext.StorageEntries.AsNoTracking().FirstOrDefaultAsync(e => e.Key == key, cancellationToken: cancellationToken);
        return entry?.Value ?? throw new KeyNotFoundException($"Key '{key}' not found");
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ReQuantumMauiDbContext>();
        var entry = await dbContext.StorageEntries.FindAsync([key], cancellationToken);
        if (entry is not null)
        {
            dbContext.StorageEntries.Remove(entry);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
