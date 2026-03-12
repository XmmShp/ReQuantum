using NOF.Contract;
using System.Text.Json;

namespace ReQuantum.Shared.Services;

public static class StorageExtensions
{
    extension(IStorage storage)
    {
        public async ValueTask<T?> GetAsync<T>(string key)
        {
            var value = await storage.GetAsync(key);
            return JsonSerializer.Deserialize<T>(value);
        }

        public async ValueTask<Optional<T?>> TryGetAsync<T>(string key)
        {
            try
            {
                return await storage.GetAsync<T>(key);
            }
            catch
            {
                return Optional.None;
            }
        }

        public async ValueTask SetAsync<T>(string key, T? value)
        {
            var json = JsonSerializer.Serialize(value);
            await storage.SetAsync(key, json);
        }
    }
}
