using Avalonia.Media.Imaging;
using Microsoft.Playwright;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Pta.Services;

public interface IPtaBrowserAuthService
{
    [MemberNotNullWhen(true, nameof(Email))]
    bool IsAuthenticated { get; }

    string? Email { get; }

    bool IsInitialized { get; }

    Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null);

    Task<Result> InitializeAsync(bool headless = true);
    Task<Result<Stream>> GetQrCodeAsync();
    Task<Result> SubmitPasswordLoginAsync(string email, string password);
    Task<Result<Stream?>> CheckForCaptchaAsync(); // Returns stream if captcha exists, null if not
    Task<Result> SubmitCaptchaAsync(string code);
    Task<Result<string>> WaitForLoginSuccessAsync(int timeoutSeconds = 200);
    Task<Result<string>> OpenBrowserAndWaitForLoginAsync(string email, string password, Action<string>? progressCallback = null, int timeoutSeconds = 300);

    Result LoginWithSession(string email, string password, string ptaSessionValue);
    void Logout();

    Task CleanupAsync();

    event Action? OnLogin;
    event Action? OnLogout;
}

[AutoInject(Lifetime.Singleton)]
public class PtaBrowserAuthService : IPtaBrowserAuthService, IDaemonService
{
    private readonly IStorage _storage;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private bool _isInitialized;
    private PtaState? _state;

    private const string StateKey = "Pta:State";

    public PtaBrowserAuthService(IStorage storage)
    {
        _storage = storage;
        LoadState();
    }

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;

    public string? Email => _state?.Email;

    public bool IsInitialized => _isInitialized;

    public event Action? OnLogin;
    public event Action? OnLogout;

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
             return Result.Fail($"服务内部状态异常 (Init:{_isInitialized}, Page:{_page!=null})");
        }

        try
        {
            await _page.GotoAsync("https://pintia.cn/auth/login?tab=wechatLogin", new PageGotoOptions { Timeout = 10000 });
            await _page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });

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
            await _page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

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

    /// <summary>
    /// 打开浏览器让用户手动完成登录，自动抓取 PTASession
    /// </summary>
    public async Task<Result<string>> OpenBrowserAndWaitForLoginAsync(
        string email,
        string password,
        Action<string>? progressCallback = null,
        int timeoutSeconds = 300)
    {
        try
        {
            progressCallback?.Invoke("🔧 正在初始化浏览器环境...");

            // 清理之前的浏览器实例
            await CleanupAsync();

            // 以非无头模式初始化浏览器
            var initResult = await InitializeAsync(headless: false);
            if (!initResult.IsSuccess)
            {
                return Result.Fail($"浏览器初始化失败: {initResult.Message}");
            }

            if (_page == null || _browser == null)
            {
                return Result.Fail("浏览器或页面对象为空");
            }

            progressCallback?.Invoke("🌐 正在打开 PTA 登录页面...");

            // 导航到登录页面
            await _page.GotoAsync("https://pintia.cn/auth/login", new PageGotoOptions { Timeout = 15000 });
            await _page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

            progressCallback?.Invoke("✏️ 正在自动填充账号信息...");

            // 等待登录表单加载完成
            var emailInput = _page.Locator("input[type='email'], input[placeholder*='邮箱'], input[name*='email'], input[placeholder*='Email']").First;
            await emailInput.WaitForAsync(new LocatorWaitForOptions { Timeout = 10000, State = WaitForSelectorState.Visible });

            // 自动填充邮箱和密码
            await emailInput.FillAsync(email);

            var passwordInput = _page.Locator("input[type='password']").First;
            await passwordInput.FillAsync(password);

            progressCallback?.Invoke("👆 请在浏览器中完成验证码验证并点击登录按钮");
            progressCallback?.Invoke("⏳ 等待登录完成（最多 " + timeoutSeconds + " 秒）...");

            // 等待用户完成登录（页面跳转到 dashboard）
            await _page.WaitForURLAsync("**/problem-sets/dashboard", new PageWaitForURLOptions
            {
                Timeout = timeoutSeconds * 1000
            });

            progressCallback?.Invoke("✓ 检测到登录成功！正在获取登录凭证...");

            // 等待 Cookie 写入
            await Task.Delay(1000);

            // 获取 PTASession Cookie
            if (_page.Context == null)
            {
                return Result.Fail("浏览器上下文为空，无法获取 Cookie");
            }

            var cookies = await _page.Context.CookiesAsync();
            if (cookies == null || cookies.Count == 0)
            {
                return Result.Fail("未获取到任何 Cookie");
            }

            var session = cookies.FirstOrDefault(c => c.Name == "PTASession");

            if (session != null && !string.IsNullOrWhiteSpace(session.Value))
            {
                progressCallback?.Invoke($"✓ 成功获取 PTASession (长度: {session.Value.Length})");
                progressCallback?.Invoke("🎉 登录流程完成！浏览器将在后台关闭...");

                // 在后台异步关闭浏览器，不等待完成以提升响应速度
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(500); // 短暂延迟，确保消息已显示
                        await CleanupAsync();
                    }
                    catch { /* 忽略后台清理错误 */ }
                });

                return Result.Success<string>(session.Value);
            }

            return Result.Fail($"登录失败：已跳转到 dashboard 但未获取到 PTASession Cookie（共 {cookies.Count} 个 Cookie）");
        }
        catch (TimeoutException)
        {
            var currentUrl = _page?.Url ?? "unknown";
            progressCallback?.Invoke($"✗ 登录超时（{timeoutSeconds} 秒内未完成）");
            return Result.Fail($"登录超时（{timeoutSeconds}秒内未完成登录）。当前页面: {currentUrl}");
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke($"✗ 发生错误: {ex.Message}");
            return Result.Fail($"浏览器登录失败: {ex.Message}");
        }
    }

    public async Task CleanupAsync()
    {
        try
        {
            if (_page != null && !_page.IsClosed)
            {
                await _page.CloseAsync();
            }
        }
        catch { /* 忽略关闭页面时的错误 */ }

        try
        {
            if (_browser != null && _browser.IsConnected)
            {
                await _browser.CloseAsync();
            }
        }
        catch { /* 忽略关闭浏览器时的错误 */ }

        // Playwright 实例通常保持
        _page = null;
        _browser = null;
        _isInitialized = false;
    }

    public async Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {
        var result = await ValidOrRefreshTokenAsync();
        if (!result.IsSuccess)
        {
            return Result.Fail(result.Message);
        }

        if (!IsAuthenticated)
        {
            return Result.Fail(nameof(UIText.NotLoggedIn));
        }

        var requestOptions = options ?? new RequestOptions();

        requestOptions.Cookies = requestOptions.Cookies is null
            ? [_state.PTASessionCookie]
            : requestOptions.Cookies.Concat([_state.PTASessionCookie]).ToList();

        // PTA API 需要 Accept 头来返回 JSON 格式（否则返回 Protobuf）
        requestOptions.Headers ??= new Dictionary<string, string>();
        if (!requestOptions.Headers.ContainsKey("Accept"))
        {
            requestOptions.Headers["Accept"] = "application/json, text/plain, */*";
        }

        return RequestClient.Create(requestOptions);
    }

    public Result LoginWithSession(string email, string password, string ptaSessionValue)
    {
        try
        {
            var ptaSessionCookie = new System.Net.Cookie("PTASession", ptaSessionValue, "/", "pintia.cn");
            _state = new PtaState(email, password, ptaSessionCookie);

            SaveState();
            OnLogin?.Invoke();

            return Result.Success("登录成功");
        }
        catch (Exception ex)
        {
            return Result.Fail($"登录异常: {ex.Message}");
        }
    }

    public void Logout()
    {
        OnLogout?.Invoke();
        _state = null;
        SaveState();
    }

    private async Task<bool> IsTokenValidAsync()
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        try
        {
            using var client = RequestClient.Create(new RequestOptions { Cookies = [_state.PTASessionCookie] });
            var response = await client.GetAsync("https://pintia.cn/api/users/profile");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<Result> ValidOrRefreshTokenAsync()
    {
        if (await IsTokenValidAsync())
        {
            return Result.Success("");
        }

        if (!IsAuthenticated)
        {
            return Result.Fail(nameof(UIText.NotLoggedIn));
        }

        // Session 已失效，需要用户重新登录
        Logout();
        return Result.Fail("Session 已过期，请重新登录");
    }

    private void LoadState()
    {
        _storage.TryGetWithEncryption(StateKey, out _state);
    }

    private void SaveState()
    {
        if (_state is null)
        {
            _storage.Remove(StateKey);
            return;
        }

        _storage.SetWithEncryption(StateKey, _state);
    }

    public void InitializeDaemon()
    {
        _ = ValidOrRefreshTokenAsync();
    }
}
