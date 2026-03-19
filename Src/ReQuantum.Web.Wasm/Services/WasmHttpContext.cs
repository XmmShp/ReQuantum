using ReQuantum.Application.Common.Models;

namespace ReQuantum.Web.Wasm.Services;

public class WasmHttpContext
{
    public HttpClient HttpClient { get; } = new();

    public void ReplaceCookies(IEnumerable<Cookie> cookies) { }

    public void ClearCookies() { }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
