using System.Net;
using Microsoft.Playwright;

namespace ReQuantum.ScriptSupport;

public static class ScriptUnifiedAuth
{
    public const string CasLoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    public const int DefaultNavigationTimeoutMs = 30000;
    public const int DefaultHttpTimeoutSeconds = 100;
    public const int DefaultPollIntervalMs = 1000;

    public static async Task<AuthenticatedHttpClientContext> CreateAuthenticatedHttpClientContextAsync(
        string readyHost = "service.zju.edu.cn",
        int navigationTimeoutMs = DefaultNavigationTimeoutMs,
        int httpTimeoutSeconds = DefaultHttpTimeoutSeconds,
        int pollIntervalMs = DefaultPollIntervalMs)
    {
        var playwright = await Playwright.CreateAsync();
        var launchOptions = new BrowserTypeLaunchOptions
        {
            Headless = false
        };

        var browserPath = ScriptBrowser.GetLocalBrowserPath();
        if (!string.IsNullOrWhiteSpace(browserPath))
        {
            launchOptions.ExecutablePath = browserPath;
        }

        var browser = await playwright.Chromium.LaunchAsync(launchOptions);
        var context = await browser.NewContextAsync();
        var page = await context.NewPageAsync();

        await page.GotoAsync(CasLoginUrl, new PageGotoOptions { Timeout = navigationTimeoutMs });

        IReadOnlyList<BrowserContextCookiesResult> browserCookies = [];
        while (true)
        {
            if (page.IsClosed)
            {
                break;
            }

            browserCookies = await context.CookiesAsync();
            if (HasReadyState(browserCookies, page.Url, readyHost))
            {
                break;
            }

            await Task.Delay(pollIntervalMs);
        }

        var cookieContainer = BuildCookieContainer(browserCookies);
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            CookieContainer = cookieContainer,
            UseCookies = true,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };

        var client = new HttpClient(new RedirectFollowingHandler(handler))
        {
            Timeout = TimeSpan.FromSeconds(httpTimeoutSeconds)
        };
        client.DefaultRequestHeaders.TryAddWithoutValidation(
            "User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0");

        return new AuthenticatedHttpClientContext(
            playwright,
            browser,
            context,
            page,
            client,
            handler,
            cookieContainer,
            browserPath,
            browserCookies);
    }

    public static bool HasReadyState(IReadOnlyList<BrowserContextCookiesResult> cookies, string currentUrl, string readyHost)
    {
        if (string.IsNullOrWhiteSpace(currentUrl)
            || !currentUrl.Contains(readyHost, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var hasIPlanet = cookies.Any(static cookie => cookie.Name.Equals("iPlanetDirectoryPro", StringComparison.OrdinalIgnoreCase));
        var hasReadyHostSession = cookies.Any(cookie => cookie.Name.Equals("JSESSIONID", StringComparison.OrdinalIgnoreCase)
                && cookie.Domain.Contains(readyHost, StringComparison.OrdinalIgnoreCase))
            || cookies.Any(cookie => cookie.Name.Equals("route", StringComparison.OrdinalIgnoreCase)
                && cookie.Domain.Contains(readyHost, StringComparison.OrdinalIgnoreCase));

        return hasIPlanet && hasReadyHostSession;
    }

    public static CookieContainer BuildCookieContainer(IReadOnlyList<BrowserContextCookiesResult> cookies)
    {
        var cookieContainer = new CookieContainer();
        foreach (var cookie in cookies)
        {
            foreach (var uri in GetCandidateUris(cookie))
            {
                try
                {
                    var systemCookie = new System.Net.Cookie(cookie.Name, cookie.Value, cookie.Path, cookie.Domain.TrimStart('.'));
                    if (cookie.Expires > 0)
                    {
                        try
                        {
                            systemCookie.Expires = DateTimeOffset.FromUnixTimeSeconds((long)cookie.Expires).DateTime;
                        }
                        catch
                        {
                        }
                    }

                    systemCookie.HttpOnly = cookie.HttpOnly;
                    systemCookie.Secure = cookie.Secure;
                    cookieContainer.Add(uri, systemCookie);
                }
                catch
                {
                }
            }
        }

        return cookieContainer;
    }

    public static IEnumerable<Uri> GetCandidateUris(BrowserContextCookiesResult cookie)
    {
        var normalizedDomain = cookie.Domain.TrimStart('.');
        if (Uri.TryCreate($"https://{normalizedDomain}/", UriKind.Absolute, out var domainUri))
        {
            yield return domainUri;
        }

        if (normalizedDomain.EndsWith("zju.edu.cn", StringComparison.OrdinalIgnoreCase))
        {
            yield return new Uri("https://zju.edu.cn/");
            yield return new Uri("https://zjuam.zju.edu.cn/");
            yield return new Uri("https://identity.zju.edu.cn/");
            yield return new Uri("https://service.zju.edu.cn/");
            yield return new Uri("https://courses.zju.edu.cn/");
            yield return new Uri("https://zdbk.zju.edu.cn/");
            yield return new Uri("https://eta.zju.edu.cn/");
        }
    }
}

public sealed class AuthenticatedHttpClientContext : IAsyncDisposable
{
    public AuthenticatedHttpClientContext(
        IPlaywright playwright,
        IBrowser browser,
        IBrowserContext browserContext,
        IPage page,
        HttpClient httpClient,
        HttpClientHandler httpHandler,
        CookieContainer cookieContainer,
        string? browserPath,
        IReadOnlyList<BrowserContextCookiesResult> cookies)
    {
        Playwright = playwright;
        Browser = browser;
        BrowserContext = browserContext;
        Page = page;
        HttpClient = httpClient;
        HttpHandler = httpHandler;
        CookieContainer = cookieContainer;
        BrowserPath = browserPath;
        Cookies = cookies;
    }

    public IPlaywright Playwright { get; }
    public IBrowser Browser { get; }
    public IBrowserContext BrowserContext { get; }
    public IPage Page { get; }
    public HttpClient HttpClient { get; }
    public HttpClientHandler HttpHandler { get; }
    public CookieContainer CookieContainer { get; }
    public string? BrowserPath { get; }
    public IReadOnlyList<BrowserContextCookiesResult> Cookies { get; }

    public async ValueTask DisposeAsync()
    {
        HttpClient.Dispose();
        HttpHandler.Dispose();
        await BrowserContext.CloseAsync();
        await Browser.CloseAsync();
        Playwright.Dispose();
    }
}

public sealed class RedirectFollowingHandler : DelegatingHandler
{
    public const int DefaultMaxRedirects = 20;

    public RedirectFollowingHandler(HttpMessageHandler innerHandler, int maxRedirects = DefaultMaxRedirects)
        : base(innerHandler)
    {
        MaxRedirects = maxRedirects;
    }

    public int MaxRedirects { get; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var currentRequest = await CloneRequestAsync(request, request.RequestUri, cancellationToken);

        for (var redirectCount = 0; ; redirectCount++)
        {
            var response = await base.SendAsync(currentRequest, cancellationToken);
            if (!IsRedirectResponse(response.StatusCode) || response.Headers.Location is null)
            {
                return response;
            }

            if (redirectCount >= MaxRedirects)
            {
                response.Dispose();
                currentRequest.Dispose();
                throw new HttpRequestException($"Too many redirects. Maximum allowed redirects: {MaxRedirects}.");
            }

            var redirectUri = response.Headers.Location;
            if (!redirectUri.IsAbsoluteUri)
            {
                redirectUri = new Uri(currentRequest.RequestUri!, redirectUri);
            }

            var nextMethod = GetRedirectMethod(response.StatusCode, currentRequest.Method);
            var includeBody = ShouldIncludeBody(currentRequest.Method, nextMethod);

            response.Dispose();
            currentRequest.Dispose();
            currentRequest = await CloneRequestAsync(
                request,
                redirectUri,
                cancellationToken,
                methodOverride: nextMethod,
                includeBody: includeBody);
        }
    }

    private static bool IsRedirectResponse(HttpStatusCode statusCode)
    {
        return statusCode == HttpStatusCode.Moved
            || statusCode == HttpStatusCode.Redirect
            || statusCode == HttpStatusCode.RedirectMethod
            || statusCode == HttpStatusCode.TemporaryRedirect
            || statusCode == HttpStatusCode.PermanentRedirect;
    }

    private static HttpMethod GetRedirectMethod(HttpStatusCode statusCode, HttpMethod originalMethod)
    {
        if (statusCode == HttpStatusCode.RedirectMethod)
        {
            return HttpMethod.Get;
        }

        if ((statusCode == HttpStatusCode.Moved || statusCode == HttpStatusCode.Redirect)
            && originalMethod != HttpMethod.Get
            && originalMethod != HttpMethod.Head)
        {
            return HttpMethod.Get;
        }

        return originalMethod;
    }

    private static bool ShouldIncludeBody(HttpMethod originalMethod, HttpMethod redirectedMethod)
    {
        if (originalMethod == HttpMethod.Get || originalMethod == HttpMethod.Head)
        {
            return false;
        }

        if (redirectedMethod == HttpMethod.Get || redirectedMethod == HttpMethod.Head)
        {
            return false;
        }

        return true;
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(
        HttpRequestMessage template,
        Uri? requestUri,
        CancellationToken cancellationToken,
        HttpMethod? methodOverride = null,
        bool includeBody = true)
    {
        var clone = new HttpRequestMessage(methodOverride ?? template.Method, requestUri)
        {
            Version = template.Version,
            VersionPolicy = template.VersionPolicy
        };

        foreach (var header in template.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (includeBody && template.Content is not null)
        {
            var contentBytes = await template.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentClone = new ByteArrayContent(contentBytes);
            foreach (var header in template.Content.Headers)
            {
                contentClone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = contentClone;
        }

        foreach (var option in template.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        return clone;
    }
}
