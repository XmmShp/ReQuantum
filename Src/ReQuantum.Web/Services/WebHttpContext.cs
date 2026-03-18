using ReQuantum.Application.Common.Services;
using System.Net;
using Cookie = System.Net.Cookie;

namespace ReQuantum.Web.Services;

public class WebHttpContext : IHttpContext
{
    private const string CookieStateKey = "ZjuSso:Cookie";
    private readonly IStorage _storage;

    public CookieContainer CookieContainer { get; } = new();
    public HttpClient HttpClient { get; }

    public WebHttpContext(IStorage storage)
    {
        _storage = storage;
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

        _ = _storage.RemoveAsync(CookieStateKey);
    }

    public async ValueTask InitializeAsync()
    {
        var state = await _storage.TryGetAsync<CookieRecord>(CookieStateKey);
        var record = state.ValueOr((CookieRecord?)null);
        if (record is null)
        {
            return;
        }

        try
        {
            var uri = new Uri($"https://{record.Domain}/");
            CookieContainer.Add(uri, new Cookie(record.Name, record.Value, record.Path, record.Domain));
        }
        catch { }
    }

    internal void SetCookie(Cookie cookie)
    {
        try
        {
            var uri = new Uri($"https://{cookie.Domain}/");
            CookieContainer.Add(uri, cookie);
        }
        catch { }

        _ = _storage.SetAsync(CookieStateKey, CookieRecord.From(cookie));
    }

    private sealed record CookieRecord(string Name, string Value, string Path, string Domain)
    {
        public static CookieRecord From(Cookie cookie) =>
            new(cookie.Name, cookie.Value, cookie.Path, cookie.Domain);
    }
}
