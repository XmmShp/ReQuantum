using ReQuantum.Application.Abstraction;
using ReQuantum.Application.Services;
using ReQuantum.Infrastructure.Abstraction;
using ReQuantum.Infrastructure.Utilities;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ReQuantum.Infrastructure.Services;

/// <summary>
/// MAUI 实现的全局 HTTP 上下文。持有唯一 <see cref="HttpClient"/> 实例，每次请求后自动将 <see cref="CookieContainer"/> 状态持久化。
/// </summary>
public sealed class MauiHttpContext : IHttpContext, IAsyncDisposable
{
    private readonly IStorage _storage;
    private const string CookieStateKey = "HttpContext:Cookies";

    public HttpClient HttpClient { get; }
    public CookieContainer CookieContainer { get; }

    public MauiHttpContext(IStorage storage)
    {
        _storage = storage;
        CookieContainer = new CookieContainer();

        var innerHandler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            CookieContainer = CookieContainer,
            UseCookies = true,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        HttpClient = new HttpClient(new PersistHandler(new RedirectFollowingHandler(innerHandler), this))
        {
            Timeout = TimeSpan.FromSeconds(100)
        };
        HttpClient.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0");
    }

    public void ReplaceCookies(IEnumerable<CookieEntry> cookies)
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

        _ = _storage.RemoveAsync(CookieStateKey);
    }

    public async ValueTask InitializeAsync()
    {
        var encryptedData = await _storage.TryGetAsync<byte[]>(CookieStateKey);
        if (!encryptedData.HasValue || encryptedData.Value is null)
        {
            return;
        }

        var decrypted = Encryption.Decrypt(encryptedData.Value);
        if (decrypted is null)
        {
            return;
        }

        List<CookieRecord>? records;
        try
        {
            records = JsonSerializer.Deserialize<List<CookieRecord>>(decrypted);
        }
        catch
        {
            return;
        }

        if (records is null)
        {
            return;
        }

        foreach (var record in records)
        {
            try
            {
                var normalizedDomain = record.Domain.TrimStart('.');
                var uri = new Uri($"https://{normalizedDomain}/");
                var cookie = new System.Net.Cookie(record.Name, record.Value, record.Path, normalizedDomain);
                if (record.Expires > 0)
                {
                    try
                    { cookie.Expires = DateTimeOffset.FromUnixTimeSeconds(record.Expires).DateTime; }
                    catch { }
                }

                cookie.HttpOnly = record.HttpOnly;
                cookie.Secure = record.Secure;
                CookieContainer.Add(uri, cookie);
            }
            catch { }
        }
    }

    internal async Task PersistAsync()
    {
        try
        {
            var cookies = CookieContainer.GetAllCookies();
            var records = cookies.Select(CookieRecord.From).ToList();
            var json = JsonSerializer.SerializeToUtf8Bytes(records);
            var encrypted = Encryption.Encrypt(json);
            await _storage.SetAsync(CookieStateKey, encrypted);
        }
        catch { }
    }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        await ValueTask.CompletedTask;
    }

    private sealed class PersistHandler : DelegatingHandler
    {
        private readonly MauiHttpContext _context;

        public PersistHandler(HttpMessageHandler inner, MauiHttpContext context) : base(inner)
        {
            _context = context;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            _ = _context.PersistAsync();
            return response;
        }
    }

    private sealed record CookieRecord
    {
        [JsonPropertyName("n")] public string Name { get; init; } = string.Empty;
        [JsonPropertyName("v")] public string Value { get; init; } = string.Empty;
        [JsonPropertyName("d")] public string Domain { get; init; } = string.Empty;
        [JsonPropertyName("p")] public string Path { get; init; } = "/";
        [JsonPropertyName("e")] public long Expires { get; init; }
        [JsonPropertyName("h")] public bool HttpOnly { get; init; }
        [JsonPropertyName("s")] public bool Secure { get; init; }

        public static CookieRecord From(System.Net.Cookie c) => new()
        {
            Name = c.Name,
            Value = c.Value,
            Domain = c.Domain,
            Path = c.Path,
            Expires = c.Expires != DateTime.MinValue ? new DateTimeOffset(c.Expires).ToUnixTimeSeconds() : 0,
            HttpOnly = c.HttpOnly,
            Secure = c.Secure
        };
    }
}
