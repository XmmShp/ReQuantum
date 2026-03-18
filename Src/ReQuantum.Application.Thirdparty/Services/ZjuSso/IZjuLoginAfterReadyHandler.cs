using NOF.Contract;

namespace ReQuantum.Application.Services.ZjuSso;

/// <summary>
/// ZJU SSO 登录完成后的回调上下文。
/// </summary>
public interface IZjuLoginAfterReadyContext
{
    /// <summary>
    /// 在当前已完成认证的浏览器上下文中访问指定 URL。
    /// </summary>
    Task<Result> VisitAsync(string url, CancellationToken cancellationToken = default);
}

/// <summary>
/// 在 ZJU SSO 浏览器登录完成且认证状态已就绪后执行附加处理。
/// </summary>
public interface IZjuLoginAfterReadyHandler
{
    /// <summary>
    /// 在登录完成后执行附加处理。
    /// </summary>
    Task<Result> OnAfterReadyAsync(IZjuLoginAfterReadyContext context, CancellationToken cancellationToken = default);
}
