using NOF.Contract;

namespace ReQuantum.Application.Services.ZjuSso;

/// <summary>
/// 提供 ZJU SSO 认证态附加到请求客户端的能力。
/// </summary>
public interface IZjuAuthenticator
{
    /// <summary>
    /// 将当前认证态应用到指定客户端。
    /// </summary>
    Task<Result> AuthorizeAsync(HttpClient client, CancellationToken cancellationToken = default);
}
