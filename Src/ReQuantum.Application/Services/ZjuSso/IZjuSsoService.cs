using System.Diagnostics.CodeAnalysis;
using NOF.Contract;
using ReQuantum.Shared.Services;

namespace ReQuantum.Application.Services.ZjuSso;

public interface IZjuSsoService
{
    [MemberNotNullWhen(true, nameof(Id))]
    bool IsAuthenticated { get; }
    string? Id { get; }
    Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null);
    Task<Result> LoginAsync(string username, string password);
    Task<Result> OpenBrowserAndWaitForLoginAsync(Action<string>? progressCallback = null, int timeoutSeconds = 300);
    Result LoginWithSession(string userId, string cookieValue);
    void Logout();
    event Action? OnLogin;
    event Action? OnLogout;
}
