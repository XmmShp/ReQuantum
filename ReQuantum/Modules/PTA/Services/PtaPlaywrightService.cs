using Avalonia.Media.Imaging;
using Microsoft.Playwright;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Modules.Common.Attributes;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Pta.Services;

public interface IPtaPlaywrightService
{
    bool IsInitialized { get; }
    Task<Result> InitializeAsync(bool headless = true);
    Task<Result<Stream>> GetQrCodeAsync();
    Task<Result> SubmitPasswordLoginAsync(string email, string password);
    Task<Result<Stream?>> CheckForCaptchaAsync(); // Returns stream if captcha exists, null if not
    Task<Result> SubmitCaptchaAsync(string code);
    Task<Result<string>> WaitForLoginSuccessAsync(int timeoutSeconds = 200);
    Task CleanupAsync();
}

[AutoInject(Lifetime.Singleton)]
public class PtaPlaywrightService : IPtaPlaywrightService
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private bool _isInitialized;

    public bool IsInitialized => _isInitialized;

    public async Task<Result> InitializeAsync(bool headless = true)
    {
        try
        {
            if (_playwright == null)
            {
                _playwright = await Playwright.CreateAsync();
            }

            if (_browser == null)
            {
                _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = headless
                });
            }

            if (_page == null)
            {
                _page = await _browser.NewPageAsync();
            }

            _isInitialized = true;
            return Result.Success("初始化成功");
        }
        catch (Exception ex)
        {
            return Result.Fail($"Playwright 初始化失败: {ex.Message}");
        }
    }

    public async Task<Result<Stream>> GetQrCodeAsync()
    {
        if (!_isInitialized || _page == null)
        {
            // 尝试自动重新初始化
            var initResult = await InitializeAsync();
            if (!initResult.IsSuccess)
            {
                return Result.Fail($"服务未初始化且自动重试失败: {initResult.Message}");
            }
        }

        if (!_isInitialized || _page == null) 
        {
             return Result.Fail($"服务内部状态异常 (Init:{_isInitialized}, Page:{_page!=null})");
        }

        try
        {
            await _page.GotoAsync("https://pintia.cn/auth/login?tab=wechatLogin");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 查找 QR 码图片
            // 策略：查找 src 为 data:image 的图片，或者包含 qrcode 的图片
            var imgLocator = _page.Locator("img[src^='data:image']").First;
            
            // 等待图片出现
            await imgLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = 5000 });
            
            var src = await imgLocator.GetAttributeAsync("src");
            if (string.IsNullOrEmpty(src))
            {
                return Result.Fail("未找到二维码图片");
            }

            var base64 = src.Split(',')[1];
            var bytes = Convert.FromBase64String(base64);
            return Result.Success<Stream>(new MemoryStream(bytes));
        }
        catch (Exception ex)
        {
            return Result.Fail($"获取二维码失败: {ex.Message}");
        }
    }

    public async Task<Result> SubmitPasswordLoginAsync(string email, string password)
    {
        if (!_isInitialized || _page == null) return Result.Fail("服务未初始化");

        try
        {
            await _page.GotoAsync("https://pintia.cn/auth/login");
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // 等待登录表单加载完成，增加超时时间到 60 秒
            var emailInput = _page.Locator("input[type='email'], input[placeholder*='邮箱'], input[name*='email'], input[placeholder*='Email']").First;
            await emailInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 60000, State = WaitForSelectorState.Visible });

            // 填写邮箱
            await emailInput.FillAsync(email);

            // 填写密码
            var passwordInput = _page.Locator("input[type='password']").First;
            await passwordInput.FillAsync(password);

            // 等待一小段时间确保输入完成
            await Task.Delay(500);

            // 点击登录按钮 - 使用多种选择器尝试
            var loginBtn = _page.Locator("button[type='submit'], button:has-text('登录'), button:has-text('Login')").First;
            await loginBtn.ClickAsync();

            return Result.Success("提交成功");
        }
        catch (Exception ex)
        {
            return Result.Fail($"提交登录失败: {ex.Message}");
        }
    }

    public async Task<Result<Stream?>> CheckForCaptchaAsync()
    {
        if (!_isInitialized || _page == null) return Result.Fail("服务未初始化");

        try
        {
            // 等待一小段时间看是否有验证码弹出
            // 验证码通常是一个图片
            // 假设验证码图片选择器
            var captchaImg = _page.Locator("img[alt*='captcha'], img[src*='captcha']");
            
            try 
            {
                await captchaImg.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 3000 });
            }
            catch (TimeoutException)
            {
                // 没有验证码
                return Result.Success<Stream?>(null);
            }

            if (await captchaImg.CountAsync() > 0)
            {
                // 截取验证码图片
                // 可以直接截图该元素
                var bytes = await captchaImg.First.ScreenshotAsync();
                return Result.Success<Stream?>(new MemoryStream(bytes));
            }

            return Result.Success<Stream?>(null);
        }
        catch (Exception ex)
        {
            return Result.Fail($"检查验证码失败: {ex.Message}");
        }
    }

    public async Task<Result> SubmitCaptchaAsync(string code)
    {
        if (!_isInitialized || _page == null) return Result.Fail("服务未初始化");

        try
        {
            // 假设验证码输入框
            var input = _page.Locator("input[placeholder*='验证码'], input[name*='captcha']");
            await input.FillAsync(code);

            // 再次点击登录或确认
            // 通常验证码输入后有确认按钮，或者是原来的登录按钮
            var confirmBtn = _page.Locator("button:has-text('确认'), button:has-text('确定')");
            if (await confirmBtn.CountAsync() > 0 && await confirmBtn.IsVisibleAsync())
            {
                await confirmBtn.ClickAsync();
            }
            else
            {
                // 尝试再次点击登录
                var loginBtn = _page.Locator("button[type='submit']");
                await loginBtn.ClickAsync();
            }

            return Result.Success("验证码提交成功");
        }
        catch (Exception ex)
        {
            return Result.Fail($"提交验证码失败: {ex.Message}");
        }
    }

    public async Task<Result<string>> WaitForLoginSuccessAsync(int timeoutSeconds = 200)
    {
        if (!_isInitialized || _page == null)
            return Result.Fail("服务未初始化");

        try
        {
            // 等待页面跳转到 dashboard（登录成功的标志）
            await _page.WaitForURLAsync("**/problem-sets/dashboard", new PageWaitForURLOptions
            {
                Timeout = timeoutSeconds * 1000
            });

            // 跳转成功后，稍等片刻确保 Cookie 已写入
            await Task.Delay(1000);

            // 检查 Context 是否为 null
            if (_page.Context == null)
            {
                return Result.Fail("浏览器上下文为空，无法获取 Cookie");
            }

            // 获取 PTASession Cookie
            var cookies = await _page.Context.CookiesAsync();

            if (cookies == null || cookies.Count == 0)
            {
                return Result.Fail("未获取到任何 Cookie");
            }

            var session = cookies.FirstOrDefault(c => c.Name == "PTASession");

            if (session != null && !string.IsNullOrWhiteSpace(session.Value))
            {
                return Result.Success<string>(session.Value);
            }

            return Result.Fail($"登录失败：已跳转到 dashboard 但未获取到 PTASession Cookie（共 {cookies.Count} 个 Cookie）");
        }
        catch (TimeoutException)
        {
            var currentUrl = _page?.Url ?? "unknown";
            return Result.Fail($"登录超时（{timeoutSeconds}秒内未跳转到 dashboard）。当前页面: {currentUrl}");
        }
        catch (NullReferenceException ex)
        {
            return Result.Fail($"空引用异常: {ex.Message}");
        }
        catch (Exception ex)
        {
            var currentUrl = _page?.Url ?? "unknown";
            return Result.Fail($"等待登录结果失败: {ex.Message}。当前页面: {currentUrl}");
        }
    }

    public async Task CleanupAsync()
    {
        if (_page != null) await _page.CloseAsync();
        if (_browser != null) await _browser.CloseAsync();
        // Playwright 实例通常保持
        _page = null;
        _browser = null;
        _isInitialized = false;
    }
}
