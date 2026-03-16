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
}

public interface IMutableZjuContext : IZjuContext
{
    Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default);

    void Logout();
}

public abstract class ZjuContext : IMutableZjuContext
{
    private readonly IStorage _storage;
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

    public abstract Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default);

    protected async Task<Result> SetAuthenticatedStateAsync(Cookie cookie, ZjuLoginInfo loginInfo)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(cookie.Value)
                || string.IsNullOrWhiteSpace(loginInfo.UserName)
                || string.IsNullOrWhiteSpace(loginInfo.LoginName)
                || string.IsNullOrWhiteSpace(loginInfo.UserId))
            {
                return Result.Fail("400", "登录信息不完整");
            }

            _state = new ZjuSsoState
            {
                UserName = loginInfo.UserName,
                LoginName = loginInfo.LoginName,
                UserId = loginInfo.UserId,
                IPlanetDirectoryPro = cookie
            };
            await SaveStateAsync();
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

    protected ValueTask EnsureStateLoadedAsync()
    {
        return LoadStateAsync();
    }

    protected async Task<ZjuSsoState?> GetStateAsync()
    {
        await LoadStateAsync();
        return _state;
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
