namespace ReQuantum.Shared.Services;

public interface IStorage
{
    bool ContainsKey(string key);
    void SetString(string key, string value);
    string GetString(string key);
    void Remove(string key);
}
