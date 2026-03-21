using ReQuantum.Application.ZjuSso.Models;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.UI.Services;

public interface IZjuContext
{
    [MemberNotNullWhen(true, nameof(LoginInfo))]
    bool IsAuthenticated => LoginInfo is not null;

    ZjuLoginInfo? LoginInfo { get; set; }

    event Action? LoggedIn;
    event Action? LoggedOut;
}

public sealed class ZjuContext : IZjuContext
{
    public ZjuLoginInfo? LoginInfo
    {
        get;
        set
        {
            if (value is null)
            {
                LoggedOut?.Invoke();
            }
            else
            {
                LoggedIn?.Invoke();
            }

            field = value;
        }
    }

    public event Action? LoggedIn;
    public event Action? LoggedOut;
}
