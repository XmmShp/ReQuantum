using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Microsoft.Playwright;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Modules.Common.Attributes;

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
                var browserPath = GetLocalBrowserPath();
                var options = new BrowserTypeLaunchOptions
                {
                    Headless = headless
                };

                // 如果找到本地浏览器，使用本地浏览器路径
                if (!string.IsNullOrEmpty(browserPath))
                {
                    options.ExecutablePath = browserPath;
                }

                _browser = await _playwright.Chromium.LaunchAsync(options);
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

    /// <summary>
    /// 检测并返回本地 Chromium 系浏览器路径
    /// </summary>
    private string? GetLocalBrowserPath()
    {
        var possiblePaths = new List<string>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // macOS 路径
            possiblePaths.AddRange(new[]
            {
                "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
                "/Applications/Microsoft Edge.app/Contents/MacOS/Microsoft Edge",
                "/Applications/Chromium.app/Contents/MacOS/Chromium",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Applications/Google Chrome.app/Contents/MacOS/Google Chrome")
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows 路径
            possiblePaths.AddRange(new[]
            {
                @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Google\Chrome\Application\chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    @"Google\Chrome\Application\chrome.exe")
            });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Linux 路径
            possiblePaths.AddRange(new[]
            {
                "/usr/bin/google-chrome",
                "/usr/bin/google-chrome-stable",
                "/usr/bin/chromium",
                "/usr/bin/chromium-browser",
                "/snap/bin/chromium",
                "/usr/bin/microsoft-edge",
                "/usr/bin/microsoft-edge-stable"
            });
        }

        // 返回第一个存在的浏览器路径
        return possiblePaths.FirstOrDefault(File.Exists);
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
            return Result.Fail($"服务内部状态异常 (Init:{_isInitialized}, Page:{_page != null})");
        }

        try
        {
            await _page.GotoAsync("https://pintia.cn/auth/login?tab=wechatLogin", new PageGotoOptions { Timeout = 10000 });
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

            // 查找 QR 码图片
            // 策略：查找 src 为 data:image 的图片，或者包含 qrcode 的图片
            var imgLocator = _page.Locator("img[src^='data:image']").First;

            // 等待图片出现
            await imgLocator.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000 });

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
        if (!_isInitialized || _page == null)
        {
            // 尝试自动重新初始化
            var initResult = await InitializeAsync();
            if (!initResult.IsSuccess)
            {
                return Result.Fail($"服务未初始化且自动重试失败: {initResult.Message}");
            }
        }

        try
        {
            // 检查浏览器和页面是否仍然有效
            if (_browser == null || _page == null)
            {
                return Result.Fail("浏览器或页面对象为空");
            }

            // 检查浏览器是否已连接
            if (!_browser.IsConnected)
            {
                return Result.Fail("浏览器已断开连接，请重新初始化");
            }

            // 检查页面是否已关闭
            if (_page.IsClosed)
            {
                // 尝试创建新页面
                _page = await _browser.NewPageAsync();
            }

            await _page.GotoAsync("https://pintia.cn/auth/login", new PageGotoOptions { Timeout = 15000 });
            await _page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

            // 等待登录表单加载完成
            var emailInput = _page.Locator("input[type='email'], input[placeholder*='邮箱'], input[name*='email'], input[placeholder*='Email']").First;
            await emailInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000, State = WaitForSelectorState.Visible });

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
            await Task.Delay(2000);

            // 腾讯云滑动验证码的常见特征：
            // 1. iframe 包含 captcha/tcaptcha
            // 2. div.tcaptcha-transform
            // 3. canvas 元素（拼图验证码）
            // 4. 显示在正中间的遮罩层

            var captchaSelectors = new[]
            {
                "#tcaptcha_iframe",                 // 腾讯云验证码特定 ID
                "iframe[src*='captcha']",           // 腾讯云验证码 iframe
                "iframe[src*='tcaptcha']",          // 腾讯云验证码 iframe（另一种）
                "div[id*='tcaptcha']",              // 腾讯云验证码容器（by id）
                "div[class*='tcaptcha']",           // 腾讯云验证码容器（by class）
                "div[class*='captcha-popup']",      // 验证码弹窗
            };

            foreach (var selector in captchaSelectors)
            {
                try
                {
                    var element = _page.Locator(selector);
                    await element.First.WaitForAsync(new LocatorWaitForOptions { Timeout = 3000 });

                    if (await element.CountAsync() > 0)
                    {
                        // 检测到验证码，尝试截图整个页面中央区域（验证码通常在中间）
                        // 由于腾讯云验证码可能在 iframe 中，我们截取页面的中央区域
                        var screenshot = await _page.ScreenshotAsync(new PageScreenshotOptions
                        {
                            Type = ScreenshotType.Png
                        });

                        return Result.Success<Stream?>(new MemoryStream(screenshot));
                    }
                }
                catch (TimeoutException)
                {
                    // 当前选择器未找到，继续尝试下一个
                    continue;
                }
            }

            // 所有选择器都未找到验证码
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
