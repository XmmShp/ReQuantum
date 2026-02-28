using ReQuantum.Shared.Services;

namespace ReQuantum.Web.Services;

public class WebStorage : IStorage
{
    private readonly Dictionary<string, string> _storage = new();

    public bool ContainsKey(string key)
    {
        return _storage.ContainsKey(key);
    }

    public void SetString(string key, string value)
    {
        _storage[key] = value;
    }

    public string GetString(string key)
    {
        if (_storage.TryGetValue(key, out var value))
        {
            return value;
        }
        throw new KeyNotFoundException($"Key '{key}' not found");
    }

    public void Remove(string key)
    {
        _storage.Remove(key);
    }
}
