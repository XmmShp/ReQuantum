using NOF.Contract;
using ReQuantum.Application.Models.ZjuSso;
using ReQuantum.Shared.Services;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace ReQuantum.Application.Services.ZjuSso;

public interface IZjuContext
{
    [MemberNotNullWhen(true, nameof(Id), nameof(SessionCookie))]
    bool IsAuthenticated { get; }

    string? Id { get; }

    Cookie? SessionCookie { get; }

    event Action? OnLogin;

    event Action? OnLogout;

    Task<Result> EnsureAuthenticatedAsync();
}

public interface IMutableZjuContext : IZjuContext
{
    Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default);

    Result Set(string userId, string cookieValue);

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

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;

    public string? Id => _state?.Id;

    public Cookie? SessionCookie => _state?.IPlanetDirectoryPro;

    public void Logout()
    {
        OnLogout?.Invoke();
        _state = null;
        _ = SaveStateAsync();
    }

    public event Action? OnLogin;
    public event Action? OnLogout;

    public async Task<Result> EnsureAuthenticatedAsync()
    {
        await LoadStateAsync();
        return await ValidOrRefreshTokenAsync();
    }

    public abstract Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default);

    public Result Set(string userId, string cookieValue)
    {
        try
        {
            var cookie = new Cookie("iPlanetDirectoryPro", cookieValue, "/", "zju.edu.cn");
            _state = new ZjuSsoState(userId, cookie);
            _ = SaveStateAsync();
            OnLogin?.Invoke();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"登录异常: {ex.Message}");
        }
    }

    private async Task<bool> IsTokenValidAsync()
    {
        await LoadStateAsync();

        if (!IsAuthenticated)
        {
            return false;
        }

        using var client = HttpClientUtilities.Create(new RequestOptions { AllowRedirects = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, LoginUrl);
        HttpClientUtilities.ApplyCookies(request, [_state.IPlanetDirectoryPro]);
        var response = await client.SendAsync(request);
        return response.StatusCode == HttpStatusCode.Redirect;
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
