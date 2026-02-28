using NOF.Contract;
using ReQuantum.Application.Models.Pta;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

namespace ReQuantum.Application.Services.Pta;

public class PtaBrowserAuthService : IPtaBrowserAuthService
{
    private readonly IStorage _storage;
    private readonly IBrowserLoginProvider _browserLoginProvider;
    private PtaState? _state;
    private const string StateKey = "Pta:State";

    public PtaBrowserAuthService(IStorage storage, IBrowserLoginProvider browserLoginProvider)
    {
        _storage = storage;
        _browserLoginProvider = browserLoginProvider;
        LoadState();
    }

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;

    public string? Email => _state?.Email;

    public event Action? OnLogin;
    public event Action? OnLogout;

    public async Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {
        var result = await ValidOrRefreshTokenAsync();
        if (!result.IsSuccess)
        {
            return Result.Fail(400, result.Message);
        }

        if (!IsAuthenticated)
        {
            return Result.Fail(400, "未登录");
        }

        var requestOptions = options ?? new RequestOptions();
        requestOptions.Cookies = requestOptions.Cookies is null
            ? [_state.PTASessionCookie]
            : requestOptions.Cookies.Concat([_state.PTASessionCookie]).ToList();

        requestOptions.Headers ??= new Dictionary<string, string>();
        if (!requestOptions.Headers.ContainsKey("Accept"))
        {
            requestOptions.Headers["Accept"] = "application/json, text/plain, */*";
        }

        return RequestClient.Create(requestOptions);
    }

    public async Task<Result> OpenBrowserAndWaitForLoginAsync(Action<string>? progressCallback = null, int timeoutSeconds = 300)
    {
        try
        {
            progressCallback?.Invoke("正在初始化浏览器环境...");

            var loginResult = await _browserLoginProvider.OpenBrowserAndWaitForCookieAsync(
                "https://pintia.cn/auth/login", "PTASession", progressCallback, timeoutSeconds);
            if (!loginResult.IsSuccess)
            {
                return Result.Fail(400, $"浏览器登录失败: {loginResult.Message}");
            }

            var result = loginResult.Value!;

            progressCallback?.Invoke("正在获取用户信息...");
            var userInfoResult = await GetUserInfoAsync(result.CookieValue);
            var username = userInfoResult.IsSuccess ? userInfoResult.Value! : "PTA用户";

            progressCallback?.Invoke($"登录成功！欢迎 {username}");
            LoginWithSession(username, result.CookieValue);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail(400, $"浏览器登录失败: {ex.Message}");
        }
    }

    public Result LoginWithSession(string email, string ptaSessionValue)
    {
        try
        {
            var ptaSessionCookie = new Cookie("PTASession", ptaSessionValue, "/", "pintia.cn");
            _state = new PtaState(email, ptaSessionCookie);
            SaveState();
            OnLogin?.Invoke();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail(400, $"登录异常: {ex.Message}");
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
        catch { return false; }
    }

    private async Task<Result> ValidOrRefreshTokenAsync()
    {
        if (await IsTokenValidAsync())
        {
            return Result.Success();
        }

        if (!IsAuthenticated)
        {
            return Result.Fail(400, "未登录");
        }

        Logout();
        return Result.Fail(400, "Session 已过期，请重新登录");
    }

    private static async Task<Result<string>> GetUserInfoAsync(string ptaSessionValue)
    {
        try
        {
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://passport.pintia.cn/api/u/current");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Cookie", $"PTASession={ptaSessionValue}");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail(400, $"获取用户信息失败: HTTP {response.StatusCode}");
            }

            var userInfo = await response.Content.ReadFromJsonAsync<PtaUserInfoResponse>();
            if (userInfo?.User?.Nickname is { Length: > 0 })
            {
                return userInfo.User.Nickname;
            }

            if (userInfo?.User?.Email is { Length: > 0 })
            {
                return userInfo.User.Email;
            }

            return Result.Fail(400, "未能获取用户信息");
        }
        catch (Exception ex)
        {
            return Result.Fail(400, $"获取用户信息异常: {ex.Message}");
        }
    }

    private void LoadState() => _storage.TryGet(StateKey, out _state);

    private void SaveState()
    {
        if (_state is null)
        { _storage.Remove(StateKey); return; }
        _storage.Set(StateKey, _state);
    }
}
