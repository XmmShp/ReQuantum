using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.ZjuSso.Models;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.UI.Services;

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
    Task<Result> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

    Task<Result> SetLoginInfoAsync(ZjuLoginInfo loginInfo, CancellationToken cancellationToken = default);

    void Logout();
}

public abstract class ZjuContext : IMutableZjuContext, IWarmup
{
    private readonly IStorage _storage;
    private readonly IEncryptor _encryptor;
    private const string CredentialStateKey = "ZjuSso:Credentials";

    protected ZjuContext(IStorage storage, IEncryptor encryptor)
    {
        _storage = storage;
        _encryptor = encryptor;
    }

    public bool IsAuthenticated => LoginInfo is not null;

    public ZjuLoginInfo? LoginInfo { get; private set; }

    public void Logout()
    {
        OnLogout?.Invoke();
        LoginInfo = null;
        ClearCredentialStateAsync().GetAwaiter().GetResult();
    }

    public event Action? OnLogin;
    public event Action? OnLogout;

    public abstract Task<Result> LoginAsync(string username, string password, CancellationToken cancellationToken = default);

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
            OnLogin?.Invoke();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"登录异常: {ex.Message}");
        }
    }

    protected async Task PersistCredentialsAsync(string username, string password)
    {
        var state = new CredentialState(
            _encryptor.EncryptToBase64(username),
            _encryptor.EncryptToBase64(password));
        await _storage.SetAsync(CredentialStateKey, state);
    }

    private async Task ClearCredentialStateAsync()
    {
        await _storage.RemoveAsync(CredentialStateKey);
    }

    public Task WarmupAsync(CancellationToken cancellationToken = default)
    {
        return TryAutoLoginAsync(cancellationToken);
    }

    private async Task TryAutoLoginAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (IsAuthenticated)
        {
            return;
        }

        var state = await _storage.TryGetAsync<CredentialState>(CredentialStateKey, cancellationToken);
        var credentialState = state.ValueOr((CredentialState?)null);
        if (credentialState is null)
        {
            return;
        }

        var username = _encryptor.TryDecryptFromBase64(credentialState.UsernameCipher);
        var password = _encryptor.TryDecryptFromBase64(credentialState.PasswordCipher);
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            await ClearCredentialStateAsync();
            return;
        }

        var loginResult = await LoginAsync(username, password, cancellationToken);
        if (!loginResult.IsSuccess)
        {
            await ClearCredentialStateAsync();
        }
    }

    private sealed record CredentialState(string UsernameCipher, string PasswordCipher);
}
