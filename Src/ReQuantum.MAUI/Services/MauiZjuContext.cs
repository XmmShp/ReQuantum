using Microsoft.Playwright;
using NOF.Contract;
using ReQuantum.Application.Models.ZjuSso;
using ReQuantum.Application.Services;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;
using System.Text.Json;

namespace ReQuantum.Infrastructure.Services;

public class MauiZjuContext : ZjuContext
{
    private const string LoginInfoUrl = "https://service.zju.edu.cn/_web/portal/api/user/loginInfo.rst?_p=YXM9MiZ0PTUmZD0xMzMmcD0xJmY9MjImbT1OJg__";
    private const string LoginInfoReferer = "https://service.zju.edu.cn/_s2/cs_sy/main.psp";

    private readonly IHttpContext _httpContext;

    public MauiZjuContext(IStorage storage, IHttpContext httpContext) : base(storage)
    {
        _httpContext = httpContext;
        OnLogout += () => _httpContext.ClearCookies();
    }

    public override async Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var playwright = await Playwright.CreateAsync();
            var options = new BrowserTypeLaunchOptions { Headless = false };
            var browserPath = BrowserHelper.GetLocalBrowserPath();
            if (!string.IsNullOrWhiteSpace(browserPath))
            {
                options.ExecutablePath = browserPath;
            }

            await using var browser = await playwright.Chromium.LaunchAsync(options);
            var page = await browser.NewPageAsync();

            await page.GotoAsync("https://zjuam.zju.edu.cn/cas/login", new PageGotoOptions { Timeout = 15000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

            string? cookieValue = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                var cookies = await page.Context.CookiesAsync();
                var target = cookies.FirstOrDefault(static cookie
                    => cookie.Name.Equals("iPlanetDirectoryPro", StringComparison.OrdinalIgnoreCase));
                if (target is not null && !string.IsNullOrWhiteSpace(target.Value))
                {
                    cookieValue = target.Value;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                return Result.Fail("400", "登录已取消或未完成");
            }

            var loginInfo = await TryGetLoginInfoAsync(page, cancellationToken);
            var setResult = Set(cookieValue, loginInfo);
            if (setResult.IsSuccess)
            {
                var allCookies = await page.Context.CookiesAsync();
                _httpContext.ReplaceCookies(allCookies.Select(c => new CookieEntry(
                    c.Name, c.Value, c.Domain, c.Path,
                    c.Expires > 0 ? (long)c.Expires : 0,
                    c.HttpOnly, c.Secure)));
            }

            return setResult;
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("400", "登录已取消");
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"浏览器登录失败: {ex.Message}");
        }
    }

    private static async Task<ZjuLoginInfo?> TryGetLoginInfoAsync(IPage page, CancellationToken cancellationToken)
    {
        try
        {
            var response = await page.Context.APIRequest.GetAsync(LoginInfoUrl, new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["Accept"] = "*/*",
                    ["Referer"] = LoginInfoReferer,
                    ["X-Requested-With"] = "XMLHttpRequest"
                },
                Timeout = 15000
            });

            if (!response.Ok)
            {
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            var content = await response.TextAsync();
            using var document = JsonDocument.Parse(content);

            if (!document.RootElement.TryGetProperty("data", out var data))
            {
                return null;
            }

            var userName = data.TryGetProperty("userName", out var userNameElement)
                ? userNameElement.GetString()
                : null;
            var loginName = data.TryGetProperty("loginName", out var loginNameElement)
                ? loginNameElement.GetString()
                : null;
            var userId = data.TryGetProperty("userId", out var userIdElement)
                ? userIdElement.ToString()
                : null;

            if (string.IsNullOrWhiteSpace(userName)
                && string.IsNullOrWhiteSpace(loginName)
                && string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return new ZjuLoginInfo(userName, loginName, userId);
        }
        catch
        {
            return null;
        }
    }
}
