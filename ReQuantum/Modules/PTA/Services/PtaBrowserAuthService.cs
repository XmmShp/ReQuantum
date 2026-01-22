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

    Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null);

    Task<Result> OpenBrowserAndWaitForLoginAsync(Action<string>? progressCallback = null, int timeoutSeconds = 300);

    Result LoginWithSession(string email, string ptaSessionValue);
    void Logout();

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

    /// <summary>
    /// 打开浏览器让用户手动完成登录，自动抓取 PTASession 并获取用户信息
    /// </summary>
    public async Task<Result> OpenBrowserAndWaitForLoginAsync(
        Action<string>? progressCallback = null,
        int timeoutSeconds = 300)
    {
        try
        {
            progressCallback?.Invoke("正在初始化浏览器环境...");

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

            progressCallback?.Invoke("正在打开 PTA 登录页面...");

            // 导航到登录页面，让用户自行选择登录方式
            await _page.GotoAsync("https://pintia.cn/auth/login", new PageGotoOptions { Timeout = 15000 });
            await _page.WaitForLoadStateAsync(Microsoft.Playwright.LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 15000 });

            progressCallback?.Invoke("请在浏览器中选择登录方式并完成登录");
            progressCallback?.Invoke($"等待登录完成（最多 {timeoutSeconds} 秒）...");

            // 等待用户完成登录（页面跳转到 dashboard）
            await _page.WaitForURLAsync("**/problem-sets/dashboard", new PageWaitForURLOptions
            {
                Timeout = timeoutSeconds * 1000
            });

            progressCallback?.Invoke("检测到登录成功！正在获取登录凭证...");

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
                progressCallback?.Invoke($"成功获取 PTASession（长度: {session.Value.Length}）");
                progressCallback?.Invoke("正在获取用户信息...");

                // 获取用户信息
                var userInfoResult = await GetUserInfoAsync(session.Value);
                var username = userInfoResult.IsSuccess ? userInfoResult.Value : "PTA用户";

                progressCallback?.Invoke($"登录成功！欢迎 {username}");

                // 使用获取到的用户名登录
                LoginWithSession(username, session.Value);

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

                return Result.Success("登录成功");
            }

            return Result.Fail($"登录失败：已跳转到 dashboard 但未获取到 PTASession Cookie（共 {cookies.Count} 个 Cookie）");
        }
        catch (TimeoutException)
        {
            var currentUrl = _page?.Url ?? "unknown";
            progressCallback?.Invoke($"登录超时（{timeoutSeconds} 秒内未完成）");
            return Result.Fail($"登录超时（{timeoutSeconds}秒内未完成登录）。当前页面: {currentUrl}");
        }
        catch (Exception ex)
        {
            progressCallback?.Invoke($"发生错误: {ex.Message}");
            return Result.Fail($"浏览器登录失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 使用PTASession获取用户信息
    /// </summary>
    private async Task<Result<string>> GetUserInfoAsync(string ptaSessionValue)
    {
        try
        {
            using var client = new System.Net.Http.HttpClient();
            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://passport.pintia.cn/api/u/current");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Cookie", $"PTASession={ptaSessionValue}");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail($"获取用户信息失败: HTTP {response.StatusCode}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var userInfo = System.Text.Json.JsonSerializer.Deserialize<PtaUserInfoResponse>(content);

            if (userInfo?.User?.Nickname != null && !string.IsNullOrWhiteSpace(userInfo.User.Nickname))
            {
                return userInfo.User.Nickname;
            }

            // 如果nickname为空，尝试使用email
            if (userInfo?.User?.Email != null && !string.IsNullOrWhiteSpace(userInfo.User.Email))
            {
                return userInfo.User.Email;
            }

            return Result.Fail("未能从用户信息中获取昵称或邮箱");
        }
        catch (Exception ex)
        {
            return Result.Fail($"获取用户信息异常: {ex.Message}");
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

    public Result LoginWithSession(string email, string ptaSessionValue)
    {
        try
        {
            var ptaSessionCookie = new System.Net.Cookie("PTASession", ptaSessionValue, "/", "pintia.cn");
            _state = new PtaState(email, ptaSessionCookie);

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
