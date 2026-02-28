using System.Text.Json;

namespace ReQuantum.Shared.Services;

public static class StorageExtensions
{
    public static T? Get<T>(this IStorage storage, string key)
    {
        var value = storage.GetString(key);
        return JsonSerializer.Deserialize<T>(value);
    }

    public static bool TryGet<T>(this IStorage storage, string key, out T? value)
    {
        try
        {
            value = storage.Get<T>(key);
            return true;
        }
        catch
        {
            value = default;
            return false;
        }
    }

    public static void Set<T>(this IStorage storage, string key, T? value)
    {
        var json = JsonSerializer.Serialize(value);
        storage.SetString(key, json);
    }
}
