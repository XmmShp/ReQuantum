using ReQuantum.Application.Abstraction;

namespace ReQuantum.Web.Wasm.Services;

public class WasmHttpContext : IHttpContext
{
    public HttpClient HttpClient { get; } = new();

    public void ReplaceCookies(IEnumerable<CookieEntry> cookies) { }

    public void ClearCookies() { }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
