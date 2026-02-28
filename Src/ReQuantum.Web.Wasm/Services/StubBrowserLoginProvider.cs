using NOF.Contract;
using ReQuantum.Application.Services.ZjuSso;

namespace ReQuantum.Web.Wasm.Services;

/// <summary>
/// WebAssembly 环境下的浏览器登录桩实现（Playwright 无法在 WASM 中运行）
/// </summary>
public class StubBrowserLoginProvider : IBrowserLoginProvider
{
    public Task<Result<BrowserLoginResult>> OpenBrowserAndWaitForCookieAsync(
        string loginUrl,
        string targetCookieName,
        Action<string>? progressCallback = null,
        int timeoutSeconds = 300)
    {
        return Task.FromResult<Result<BrowserLoginResult>>(
            Result.Fail(500, "浏览器登录在 WebAssembly 环境中不可用，请使用手动 Cookie 登录"));
    }
}
