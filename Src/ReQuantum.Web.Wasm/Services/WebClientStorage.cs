using ReQuantum.Shared.Services;

namespace ReQuantum.Web.Wasm.Services;

public class WebClientStorage : IStorage
{
    private readonly Dictionary<string, string> _storage = new();

    public ValueTask<bool> ContainsAsync(string key, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(_storage.ContainsKey(key));
    }

    public ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        _storage[key] = value;
        return ValueTask.CompletedTask;
    }

    public ValueTask<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_storage.TryGetValue(key, out var value))
        {
            return ValueTask.FromResult(value);
        }

        throw new KeyNotFoundException($"Key '{key}' not found");
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _storage.Remove(key);
        return ValueTask.CompletedTask;
    }
}
