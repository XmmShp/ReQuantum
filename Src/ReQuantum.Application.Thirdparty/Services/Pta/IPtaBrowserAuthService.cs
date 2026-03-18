using NOF.Contract;
using ReQuantum.Shared.Services;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.Application.Services.Pta;

public interface IPtaBrowserAuthService
{
    [MemberNotNullWhen(true, nameof(Email))]
    bool IsAuthenticated { get; }

    string? Email { get; }

    Task<Result<HttpClient>> GetAuthenticatedClientAsync(RequestOptions? options = null);

    Task<Result> OpenBrowserAndWaitForLoginAsync(CancellationToken cancellationToken = default);

    Result LoginWithSession(string email, string ptaSessionValue);
    void Logout();

    event Action? OnLogin;
    event Action? OnLogout;
}
