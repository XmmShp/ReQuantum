using Microsoft.Playwright;
using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.Pta.Services;
using ReQuantum.Shared.Services;

namespace ReQuantum.Web.Services;

public class WebPtaBrowserAuthService : PtaBrowserAuthService
{
    public WebPtaBrowserAuthService(IStorage storage) : base(storage)
    {
    }

    public override async Task<Result> OpenBrowserAndWaitForLoginAsync(CancellationToken cancellationToken = default)
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

            await page.GotoAsync("https://pintia.cn/auth/login", new PageGotoOptions { Timeout = 15000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

            string? cookieValue = null;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(1000, cancellationToken);
                var cookies = await page.Context.CookiesAsync();
                var target = cookies.FirstOrDefault(static cookie => cookie.Name.Equals("PTASession", StringComparison.OrdinalIgnoreCase));
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

            var userInfoResult = await GetUserInfoAsync(cookieValue);
            var username = userInfoResult.IsSuccess ? userInfoResult.Value! : "PTA用户";
            LoginWithSession(username, cookieValue);
            return Result.Success();
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
}
