using ReQuantum.Application.Common.Models;

namespace ReQuantum.Application.Common.Services;

/// <summary>
/// 全局共享的 HTTP 上下文，持有单例 <see cref="HttpClient"/>。
/// 内部负责 Cookie 的持久化与还原，调用方只负责具体的 HTTP 交互。
/// </summary>
public interface IHttpContext
{
    /// <summary>全局共享的 <see cref="HttpClient"/> 实例，生命周期与应用相同。</summary>
    HttpClient HttpClient { get; }

    /// <summary>
    /// 替换当前所有 Cookie（清除旧 Cookie 后重新灌入）。实现方负责持久化。
    /// </summary>
    void ReplaceCookies(IEnumerable<Cookie> cookies);

    /// <summary>
    /// 清空所有 Cookie 并从持久化存储中移除。
    /// </summary>
    void ClearCookies();
}
