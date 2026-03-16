using NOF.Contract;
using ReQuantum.Application.Models.ZjuSso;

namespace ReQuantum.Application.Services.ZjuSso;

/// <summary>
/// 提供手动写入 ZJU 登录态的能力。
/// </summary>
public interface IZjuLoginStateWriter
{
    /// <summary>
    /// 将指定登录信息写入当前上下文。
    /// </summary>
    Task<Result> SetAuthenticatedStateAsync(string cookieValue, ZjuLoginInfo loginInfo, CancellationToken cancellationToken = default);
}
