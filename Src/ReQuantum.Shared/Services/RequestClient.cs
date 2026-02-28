using System.Net;

namespace ReQuantum.Shared.Services;

/// <summary>
/// HTTP请求选项类，用于配置RequestClient的行为
/// </summary>
public class RequestOptions
{
    /// <summary>
    /// 获取或设置请求的Cookie列表
    /// </summary>
    public List<Cookie>? Cookies { get; set; }

    /// <summary>
    /// 获取或设置请求超时时间（秒），默认为100秒
    /// </summary>
    public int TimeoutSeconds { get; set; } = 100;

    /// <summary>
    /// 获取或设置是否允许自动重定向，默认为false
    /// </summary>
    public bool AllowRedirects { get; set; } = false;

    /// <summary>
    /// 获取或设置请求头字典
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }
}

/// <summary>
/// HTTP请求客户端类，继承自HttpClient，提供更多的配置选项和Cookie管理功能
/// </summary>
public class RequestClient : HttpClient
{
    /// <summary>
    /// 获取Cookie容器实例
    /// </summary>
    public CookieContainer CookieContainer { get; }

    /// <summary>
    /// 创建RequestClient的私有构造函数
    /// </summary>
    private RequestClient(HttpMessageHandler handler, CookieContainer cookieContainer, RequestOptions? options = null) : base(handler)
    {
        CookieContainer = cookieContainer;
        Timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? 100);

        if (options?.Cookies is not null)
        {
            foreach (var cookie in options.Cookies)
            {
                cookieContainer.Add(cookie);
            }
        }

        DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

        if (options?.Headers is not null)
        {
            foreach (var header in options.Headers)
            {
                DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }

    /// <summary>
    /// 创建RequestClient实例的工厂方法
    /// </summary>
    public static RequestClient Create(RequestOptions? options = null)
    {
        var cookieContainer = new CookieContainer();
        var handler = new HttpClientHandler
        {
            CookieContainer = cookieContainer,
            UseCookies = true,
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            AllowAutoRedirect = options?.AllowRedirects ?? true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };

        return new RequestClient(handler, cookieContainer, options);
    }
}
