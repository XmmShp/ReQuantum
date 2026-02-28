using NOF.Contract;

namespace ReQuantum.Application.Services.ZjuSso;

/// <summary>
/// 浏览器登录结果
/// </summary>
public record BrowserLoginResult(string CookieValue, string? Username = null);

/// <summary>
/// 抽象浏览器登录提供者，避免在共享代码中直接依赖 Playwright
/// </summary>
public interface IBrowserLoginProvider
{
    Task<Result<BrowserLoginResult>> OpenBrowserAndWaitForCookieAsync(
        string loginUrl,
        string targetCookieName,
        Action<string>? progressCallback = null,
        int timeoutSeconds = 300);
}
