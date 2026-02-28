using ReQuantum.Shared.Services;

namespace ReQuantum.Services;

public class LocalStorage : IStorage
{
    private readonly Dictionary<string, string> _storage = new();
    private readonly string _storagePath;

    public LocalStorage()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "ReQuantum");
        Directory.CreateDirectory(appFolder);
        _storagePath = Path.Combine(appFolder, "storage.json");
        LoadFromFile();
    }

    public bool ContainsKey(string key)
    {
        return _storage.ContainsKey(key);
    }

    public void SetString(string key, string value)
    {
        _storage[key] = value;
        SaveToFile();
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
        SaveToFile();
    }

    private void LoadFromFile()
    {
        if (File.Exists(_storagePath))
        {
            try
            {
                var json = File.ReadAllText(_storagePath);
                var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _storage[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
            }
        }
    }

    private void SaveToFile()
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(_storage);
            File.WriteAllText(_storagePath, json);
        }
        catch
        {
        }
    }
}
