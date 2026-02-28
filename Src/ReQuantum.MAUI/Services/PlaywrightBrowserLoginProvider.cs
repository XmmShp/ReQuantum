using Microsoft.Extensions.Logging;
using NOF.Contract;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;

namespace ReQuantum.Services;

/// <summary>
/// 基于 Playwright 的浏览器登录提供者，用于桌面/MAUI 平台
/// </summary>
public class PlaywrightBrowserLoginProvider : IBrowserLoginProvider
{
    private readonly ILogger<PlaywrightBrowserLoginProvider> _logger;
    private Microsoft.Playwright.IPlaywright? _playwright;
    private Microsoft.Playwright.IBrowser? _browser;
    private Microsoft.Playwright.IPage? _page;

    public PlaywrightBrowserLoginProvider(ILogger<PlaywrightBrowserLoginProvider> logger)
    {
        _logger = logger;
    }

    public async Task<Result<BrowserLoginResult>> OpenBrowserAndWaitForCookieAsync(
        string loginUrl,
        string targetCookieName,
        Action<string>? progressCallback = null,
        int timeoutSeconds = 300)
    {
        try
        {
            progressCallback?.Invoke("正在初始化浏览器环境...");

            await CleanupAsync();

            var initResult = await InitializeAsync(headless: false);
            if (!initResult.IsSuccess)
            {
                return Result.Fail(400, $"浏览器初始化失败: {initResult.Message}");
            }

            if (_page is null || _browser is null)
            {
                return Result.Fail(400, "浏览器或页面对象为空");
            }

            progressCallback?.Invoke("正在打开登录页面...");

            await _page.GotoAsync(loginUrl, new Microsoft.Playwright.PageGotoOptions { Timeout = 15000 });
            await _page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle,
                new Microsoft.Playwright.PageWaitForLoadStateOptions { Timeout = 15000 });

            progressCallback?.Invoke("请在浏览器中完成登录");
            progressCallback?.Invoke($"等待登录完成（最多 {timeoutSeconds} 秒）...");

            // 轮询等待目标 Cookie 出现
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            string? cookieValue = null;

            while (DateTime.UtcNow < deadline)
            {
                await Task.Delay(1000);

                if (_page.Context is null) continue;

                var cookies = await _page.Context.CookiesAsync();
                var target = cookies?.FirstOrDefault(c =>
                    c.Name.Equals(targetCookieName, StringComparison.OrdinalIgnoreCase));

                if (target is not null && !string.IsNullOrWhiteSpace(target.Value))
                {
                    cookieValue = target.Value;
                    break;
                }
            }

            if (cookieValue is null)
            {
                progressCallback?.Invoke("登录超时或未获取到认证 Cookie");
                return Result.Fail(400, $"登录超时（{timeoutSeconds} 秒内未完成登录）");
            }

            progressCallback?.Invoke($"成功获取 {targetCookieName}（长度: {cookieValue.Length}）");

            // 尝试从页面获取用户名
            string? username = null;
            try
            {
                var idElement = await _page.QuerySelectorAsync("#msg .success span");
                if (idElement is not null)
                {
                    username = await idElement.TextContentAsync();
                }
            }
            catch { /* 忽略用户名获取失败 */ }

            // 后台异步关闭浏览器
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(500);
                    await CleanupAsync();
                }
                catch { /* 忽略后台清理错误 */ }
            });

            return new BrowserLoginResult(cookieValue, username);
        }
        catch (TimeoutException)
        {
            var currentUrl = _page?.Url ?? "unknown";
            progressCallback?.Invoke($"登录超时（{timeoutSeconds} 秒内未完成）");
            return Result.Fail(400, $"登录超时（{timeoutSeconds}秒内未完成登录）。当前页面: {currentUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "浏览器登录失败");
            progressCallback?.Invoke($"发生错误: {ex.Message}");
            return Result.Fail(400, $"浏览器登录失败: {ex.Message}");
        }
    }

    private async Task<Result> InitializeAsync(bool headless)
    {
        try
        {
            _playwright ??= await Microsoft.Playwright.Playwright.CreateAsync();

            if (_browser is null)
            {
                var browserPath = BrowserHelper.GetLocalBrowserPath();
                var options = new Microsoft.Playwright.BrowserTypeLaunchOptions { Headless = headless };

                if (!string.IsNullOrEmpty(browserPath))
                {
                    options.ExecutablePath = browserPath;
                }

                _browser = await _playwright.Chromium.LaunchAsync(options);
            }

            _page ??= await _browser.NewPageAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Playwright 初始化失败");
            return Result.Fail(400, $"Playwright 初始化失败: {ex.Message}");
        }
    }

    private async Task CleanupAsync()
    {
        try
        {
            if (_page is not null && !_page.IsClosed)
                await _page.CloseAsync();
        }
        catch { }

        try
        {
            if (_browser is not null && _browser.IsConnected)
                await _browser.CloseAsync();
        }
        catch { }

        _page = null;
        _browser = null;
    }
}
