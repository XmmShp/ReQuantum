using NOF.Contract;
using System.Text.Json;

namespace ReQuantum.Application.Abstraction;

public interface IStorage
{
    ValueTask<bool> ContainsAsync(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default);
    ValueTask<string> GetAsync(string key, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await GetAsync(key, cancellationToken);
        return JsonSerializer.Deserialize<T>(value);
    }

    async ValueTask<Optional<T?>> TryGetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await GetAsync<T>(key, cancellationToken);
        }
        catch
        {
            return Optional.None;
        }
    }

    async ValueTask SetAsync<T>(string key, T? value, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(value);
        await SetAsync(key, json, cancellationToken);
    }
}
