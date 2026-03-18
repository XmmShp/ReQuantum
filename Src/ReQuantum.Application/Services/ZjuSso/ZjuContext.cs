using NOF.Abstraction;
using NOF.Contract;
using ReQuantum.Application.Models.ZjuSso;
using ReQuantum.Shared.Services;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.Application.Services.ZjuSso;

public interface IZjuContext
{
    [MemberNotNullWhen(true, nameof(LoginInfo))]
    bool IsAuthenticated { get; }

    ZjuLoginInfo? LoginInfo { get; }

    event Action? OnLogin;

    event Action? OnLogout;
}

public interface IMutableZjuContext : IZjuContext
{
    Task<Result> LoginAsync(CancellationToken cancellationToken = default);

    Task<Result> SetLoginInfoAsync(ZjuLoginInfo loginInfo, CancellationToken cancellationToken = default);

    void Logout();
}

public abstract class ZjuContext : IMutableZjuContext, IInitializable
{
    private readonly IStorage _storage;
    private const string StateKey = "ZjuSso:State";

    protected ZjuContext(IStorage storage)
    {
        _storage = storage;
    }

    public bool IsAuthenticated => LoginInfo is not null;

    public ZjuLoginInfo? LoginInfo { get; private set; }

    public void Logout()
    {
        OnLogout?.Invoke();
        LoginInfo = null;
        SaveStateAsync().GetAwaiter().GetResult();
    }

    public event Action? OnLogin;
    public event Action? OnLogout;

    public abstract Task<Result> LoginAsync(CancellationToken cancellationToken = default);

    public async Task<Result> SetLoginInfoAsync(ZjuLoginInfo loginInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            if (string.IsNullOrWhiteSpace(loginInfo.UserName) || string.IsNullOrWhiteSpace(loginInfo.LoginName))
            {
                return Result.Fail("400", "登录信息不完整");
            }

            LoginInfo = loginInfo;
            await SaveStateAsync();
            OnLogin?.Invoke();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"登录异常: {ex.Message}");
        }
    }

    private async Task LoadStateAsync()
    {
        if (LoginInfo is not null)
        {
            return;
        }

        var state = await _storage.TryGetAsync<ZjuLoginInfo>(StateKey);
        LoginInfo = state.ValueOr((ZjuLoginInfo?)null);
    }

    private async Task SaveStateAsync()
    {
        if (LoginInfo is null)
        {
            await _storage.RemoveAsync(StateKey);
            return;
        }

        await _storage.SetAsync(StateKey, LoginInfo);
    }

    public void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }
        IsInitialized = true;
        LoadStateAsync().GetAwaiter().GetResult();
    }

    public bool IsInitialized { get; private set; }
}
