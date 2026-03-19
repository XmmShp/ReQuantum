using LoginZju;

namespace ReQuantum.Application.ZjuSso.Services;

/// <summary>
/// 持有当前已认证的 <see cref="IZjuamAuth"/> 实例，供需要 ZJUAM 认证的服务共享使用。
/// </summary>
public sealed class ZjuamAuthHolder : IDisposable
{
    private IZjuamAuth? _currentAuth;

    /// <summary>
    /// 当前已认证的 <see cref="IZjuamAuth"/> 实例。未登录时为 <c>null</c>。
    /// </summary>
    public IZjuamAuth? CurrentAuth => _currentAuth;

    /// <summary>
    /// 设置当前已认证的 <see cref="IZjuamAuth"/> 实例。会释放之前的实例。
    /// </summary>
    public void SetAuth(IZjuamAuth? auth)
    {
        var old = Interlocked.Exchange(ref _currentAuth, auth);
        if (old is not null && !ReferenceEquals(old, auth))
        {
            old.Dispose();
        }
    }

    public void Dispose()
    {
        _currentAuth?.Dispose();
        _currentAuth = null;
    }
}
