namespace ReQuantum.Shared.Services;

public interface IStorage
{
    ValueTask<bool> ContainsAsync(string key, CancellationToken cancellationToken = default);
    ValueTask SetAsync(string key, string value, CancellationToken cancellationToken = default);
    ValueTask<string> GetAsync(string key, CancellationToken cancellationToken = default);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);
}
