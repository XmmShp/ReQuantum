using System.Diagnostics.CodeAnalysis;
using ReQuantum.Shared.Models;
using ReQuantum.Shared.Services;

namespace ReQuantum.Application.Services.Pta;

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
