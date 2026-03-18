namespace ReQuantum.Application.Common.Models;

/// <summary>平台无关的 Cookie 描述，用于在 <see cref="IHttpContext"/> 中传递 Cookie 数据。</summary>
public sealed record Cookie(
    string Name,
    string Value,
    string Domain,
    string Path,
    long Expires,
    bool HttpOnly,
    bool Secure);
