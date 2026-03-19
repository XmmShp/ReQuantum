using LoginZju;
using ReQuantum.Application.ZjuSso.Models;

namespace ReQuantum.Application.ZjuSso.Services;

public interface IZjuAuthAccessor
{
    /// <summary>
    /// 当前已认证的 <see cref="IZjuamAuth"/> 实例。未登录时为 <c>null</c>。
    /// </summary>
    IZjuamAuth? Current { get; }

    /// <summary>
    /// 当前登录用户信息。未登录时为 <c>null</c>。
    /// </summary>
    ZjuLoginInfo? CurrentLoginInfo { get; }

    /// <summary>
    /// 设置当前认证会话。会释放之前的认证实例。
    /// </summary>
    void SetSession(IZjuamAuth auth, ZjuLoginInfo loginInfo);

    /// <summary>
    /// 清空当前认证会话，并释放认证实例。
    /// </summary>
    void Clear();
}

/// <summary>
/// 持有当前 ZJU 认证会话，供应用层服务共享使用。
/// </summary>
public sealed class ZjuAuthAccessor : IDisposable, IZjuAuthAccessor
{
    private IZjuamAuth? _current;

    /// <summary>
    /// 当前已认证的 <see cref="IZjuamAuth"/> 实例。未登录时为 <c>null</c>。
    /// </summary>
    public IZjuamAuth? Current => _current;

    /// <summary>
    /// 当前登录用户信息。未登录时为 <c>null</c>。
    /// </summary>
    public ZjuLoginInfo? CurrentLoginInfo { get; private set; }

    /// <summary>
    /// 设置当前认证会话。会释放之前的认证实例。
    /// </summary>
    public void SetSession(IZjuamAuth auth, ZjuLoginInfo loginInfo)
    {
        var old = Interlocked.Exchange(ref _current, auth);
        CurrentLoginInfo = loginInfo;
        if (old is not null && !ReferenceEquals(old, auth))
        {
            old.Dispose();
        }
    }

    /// <summary>
    /// 清空当前认证会话，并释放认证实例。
    /// </summary>
    public void Clear()
    {
        var old = Interlocked.Exchange(ref _current, null);
        CurrentLoginInfo = null;
        old?.Dispose();
    }

    public void Dispose()
    {
        Clear();
    }
}
