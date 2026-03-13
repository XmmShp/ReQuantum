using NOF.Contract;
using ReQuantum.Application.Models.ZjuSso;
using ReQuantum.Shared.Services;
using System.Net;

namespace ReQuantum.Application.Services.ZjuSso;

public interface IZjuContext
{
    bool IsAuthenticated { get; }

    ZjuLoginInfo? LoginInfo { get; }

    event Action? OnLogin;

    event Action? OnLogout;

    Task<Result> AuthorizeAsync(HttpClient client, CancellationToken cancellationToken = default);
}

public interface IMutableZjuContext : IZjuContext
{
    Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default);

    Result Set(string cookieValue, ZjuLoginInfo? loginInfo = null);

    void Logout();
}

public abstract class ZjuContext : IMutableZjuContext
{
    private readonly IStorage _storage;
    private const string LoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string StateKey = "ZjuSso:State";

    private ZjuSsoState? _state;

    protected ZjuContext(IStorage storage)
    {
        _storage = storage;
    }

    public bool IsAuthenticated => _state is not null;

    public ZjuLoginInfo? LoginInfo => _state is null
        ? null
        : new ZjuLoginInfo(_state.UserName, _state.LoginName, _state.UserId);

    public void Logout()
    {
        OnLogout?.Invoke();
        _state = null;
        _ = SaveStateAsync();
    }

    public event Action? OnLogin;
    public event Action? OnLogout;

    public async Task<Result> AuthorizeAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        await LoadStateAsync();

        if (!IsAuthenticated)
        {
            return Result.Fail("400", "未登录");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, LoginUrl);
        HttpClientUtilities.ApplyCookies(request, [_state!.IPlanetDirectoryPro]);
        var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Redirect)
        {
            Logout();
            return Result.Fail("400", "Session 已过期，请重新登录");
        }

        var cookieHeader = HttpClientUtilities.BuildCookieHeader([_state.IPlanetDirectoryPro]);
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
        return Result.Success();
    }

    public abstract Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default);

    public Result Set(string cookieValue, ZjuLoginInfo? loginInfo = null)
    {
        try
        {
            if (loginInfo is null
                || string.IsNullOrWhiteSpace(loginInfo.UserName)
                || string.IsNullOrWhiteSpace(loginInfo.LoginName)
                || string.IsNullOrWhiteSpace(loginInfo.UserId))
            {
                return Result.Fail("400", "登录信息不完整");
            }

            var cookie = new Cookie("iPlanetDirectoryPro", cookieValue, "/", "zju.edu.cn");
            _state = new ZjuSsoState
            {
                UserName = loginInfo.UserName,
                LoginName = loginInfo.LoginName,
                UserId = loginInfo.UserId,
                IPlanetDirectoryPro = cookie
            };
            _ = SaveStateAsync();
            OnLogin?.Invoke();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"登录异常: {ex.Message}");
        }
    }

    private async ValueTask LoadStateAsync()
    {
        if (_state is not null)
        {
            return;
        }

        var state = await _storage.TryGetAsync<ZjuSsoState>(StateKey);
        _state = state.ValueOr((ZjuSsoState?)null);
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
