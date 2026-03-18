using ReQuantum.Application.Common.Models;
using ReQuantum.Application.Common.Services;

namespace ReQuantum.Web.Wasm.Services;

public class WasmHttpContext : IHttpContext
{
    public HttpClient HttpClient { get; } = new();

    public void ReplaceCookies(IEnumerable<Cookie> cookies) { }

    public void ClearCookies() { }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;
}
