using System.Net;
using Cookie = System.Net.Cookie;

namespace ReQuantum.Web.Services;

public class WebHttpContext
{
    public CookieContainer CookieContainer { get; } = new();
    public HttpClient HttpClient { get; }

    public WebHttpContext()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            CookieContainer = CookieContainer,
            UseCookies = true,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };
        HttpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(100)
        };
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
    }

    public void ReplaceCookies(IEnumerable<Application.Common.Models.Cookie> cookies)
    {
        foreach (var entry in cookies)
        {
            try
            {
                var normalizedDomain = entry.Domain.TrimStart('.');
                var uri = new Uri($"https://{normalizedDomain}/");
                CookieContainer.Add(uri, new Cookie(entry.Name, entry.Value, entry.Path, normalizedDomain));
            }
            catch { }
        }
    }

    public void ClearCookies()
    {
        foreach (Cookie c in CookieContainer.GetAllCookies())
        {
            c.Expired = true;
        }
    }

    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    internal void SetCookie(Cookie cookie)
    {
        try
        {
            var uri = new Uri($"https://{cookie.Domain}/");
            CookieContainer.Add(uri, cookie);
        }
        catch { }
    }
}
