using ReQuantum.Infrastructure.Abstraction;
using System.Net;
using Cookie = ReQuantum.Application.Common.Models.Cookie;

namespace ReQuantum.Infrastructure.Services;

/// <summary>
/// MAUI 实现的全局 HTTP 上下文。持有唯一 <see cref="HttpClient"/> 实例，Cookie 仅保存在内存中。
/// </summary>
public sealed class MauiHttpContext : IAsyncDisposable
{
    public HttpClient HttpClient { get; }
    public CookieContainer CookieContainer { get; }

    public MauiHttpContext()
    {
        CookieContainer = new CookieContainer();

        var innerHandler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            CookieContainer = CookieContainer,
            UseCookies = true,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        HttpClient = new HttpClient(new RedirectFollowingHandler(innerHandler))
        {
            Timeout = TimeSpan.FromSeconds(100)
        };
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0");
    }

    public void ReplaceCookies(IEnumerable<Cookie> cookies)
    {
        foreach (var entry in cookies)
        {
            try
            {
                var normalizedDomain = entry.Domain.TrimStart('.');
                var uri = new Uri($"https://{normalizedDomain}/");
                var cookie = new System.Net.Cookie(entry.Name, entry.Value, entry.Path, normalizedDomain);
                if (entry.Expires > 0)
                {
                    try
                    { cookie.Expires = DateTimeOffset.FromUnixTimeSeconds(entry.Expires).DateTime; }
                    catch { }
                }

                cookie.HttpOnly = entry.HttpOnly;
                cookie.Secure = entry.Secure;
                CookieContainer.Add(uri, cookie);
            }
            catch { }
        }
    }

    public void ClearCookies()
    {
        foreach (System.Net.Cookie c in CookieContainer.GetAllCookies())
        {
            c.Expired = true;
        }
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await ValueTask.CompletedTask;
    }
}
