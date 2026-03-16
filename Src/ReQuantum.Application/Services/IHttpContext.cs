namespace ReQuantum.Application.Services;

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
    void ReplaceCookies(IEnumerable<CookieEntry> cookies);

    /// <summary>
    /// 清空所有 Cookie 并从持久化存储中移除。
    /// </summary>
    void ClearCookies();

    /// <summary>
    /// 从持久化存储还原 Cookie。应在应用启动后调用一次，幂等。
    /// </summary>
    ValueTask InitializeAsync();
}

/// <summary>平台无关的 Cookie 描述，用于在 <see cref="IHttpContext"/> 中传递 Cookie 数据。</summary>
public sealed record CookieEntry(
    string Name,
    string Value,
    string Domain,
    string Path,
    long Expires,
    bool HttpOnly,
    bool Secure);
