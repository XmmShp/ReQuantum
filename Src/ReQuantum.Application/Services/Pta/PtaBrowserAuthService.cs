using NOF.Contract;
using ReQuantum.Application.Models.Pta;
using ReQuantum.Shared.Services;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;

namespace ReQuantum.Application.Services.Pta;

public abstract class PtaBrowserAuthService : IPtaBrowserAuthService
{
    private readonly IStorage _storage;
    private PtaState? _state;
    private const string StateKey = "Pta:State";

    protected PtaBrowserAuthService(IStorage storage)
    {
        _storage = storage;
    }

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;

    public string? Email => _state?.Email;

    public event Action? OnLogin;
    public event Action? OnLogout;

    public async Task<Result<HttpClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {
        await LoadStateAsync();

        var result = await ValidOrRefreshTokenAsync();
        if (!result.IsSuccess)
        {
            return Result.Fail("400", result.Message);
        }

        if (!IsAuthenticated)
        {
            return Result.Fail("400", "未登录");
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

        return HttpClientUtilities.Create(requestOptions);
    }

    public abstract Task<Result> OpenBrowserAndWaitForLoginAsync(CancellationToken cancellationToken = default);

    public Result LoginWithSession(string email, string ptaSessionValue)
    {
        try
        {
            var ptaSessionCookie = new Cookie("PTASession", ptaSessionValue, "/", "pintia.cn");
            _state = new PtaState(email, ptaSessionCookie);
            _ = SaveStateAsync();
            OnLogin?.Invoke();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"登录异常: {ex.Message}");
        }
    }

    public void Logout()
    {
        OnLogout?.Invoke();
        _state = null;
        _ = SaveStateAsync();
    }

    private async Task<bool> IsTokenValidAsync()
    {
        await LoadStateAsync();

        if (!IsAuthenticated)
        {
            return false;
        }

        try
        {
            using var client = HttpClientUtilities.Create();
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://pintia.cn/api/users/profile");
            HttpClientUtilities.ApplyCookies(request, [_state.PTASessionCookie]);
            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private async Task<Result> ValidOrRefreshTokenAsync()
    {
        await LoadStateAsync();

        if (await IsTokenValidAsync())
        {
            return Result.Success();
        }

        if (!IsAuthenticated)
        {
            return Result.Fail("400", "未登录");
        }

        Logout();
        return Result.Fail("400", "Session 已过期，请重新登录");
    }

    protected static async Task<Result<string>> GetUserInfoAsync(string ptaSessionValue)
    {
        try
        {
            using var client = HttpClientUtilities.Create();
            var request = new HttpRequestMessage(HttpMethod.Get, "https://passport.pintia.cn/api/u/current");
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("Cookie", $"PTASession={ptaSessionValue}");

            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail("400", $"获取用户信息失败: HTTP {response.StatusCode}");
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

            return Result.Fail("400", "未能获取用户信息");
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"获取用户信息异常: {ex.Message}");
        }
    }

    private async ValueTask LoadStateAsync()
    {
        if (_state is not null)
        {
            return;
        }

        var state = await _storage.TryGetAsync<PtaState>(StateKey);
        _state = state.ValueOr((PtaState?)null);
    }

    private async ValueTask SaveStateAsync()
    {
        if (_state is null)
        {
            await _storage.RemoveAsync(StateKey);
            return;
        }

        await _storage.SetAsync(StateKey, _state);
    }
}
