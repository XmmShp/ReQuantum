using System.Net;
using Microsoft.Playwright;

namespace ReQuantum.ScriptSupport;

public static class ScriptUnifiedAuth
{
    public const int DefaultNavigationTimeoutMs = 30000;
    public const int DefaultHttpTimeoutSeconds = 100;
    public const int DefaultPollIntervalMs = 1000;

    public static async Task<AuthenticatedHttpClientContext> CreateInteractiveHttpClientContextAsync(
        InteractiveAuthOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.StartUrl);
        ArgumentNullException.ThrowIfNull(options.WaitUntil);

        var playwright = await Playwright.CreateAsync();
        try
        {
            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = options.Headless
            };

            var browserPath = string.IsNullOrWhiteSpace(options.BrowserExecutablePath)
                ? ScriptBrowser.GetLocalBrowserPath()
                : options.BrowserExecutablePath;
            if (!string.IsNullOrWhiteSpace(browserPath))
            {
                launchOptions.ExecutablePath = browserPath;
            }

            var browser = await playwright.Chromium.LaunchAsync(launchOptions);
            try
            {
                var context = await browser.NewContextAsync();
                try
                {
                    var page = await context.NewPageAsync();
                    await page.GotoAsync(options.StartUrl, new PageGotoOptions { Timeout = options.NavigationTimeoutMs });

                    IReadOnlyList<BrowserContextCookiesResult> browserCookies = [];
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if (page.IsClosed)
                        {
                            break;
                        }

                        browserCookies = await context.CookiesAsync();
                        var state = new InteractiveAuthState(context, page, browserCookies);
                        if (await options.WaitUntil(state, cancellationToken))
                        {
                            break;
                        }

                        await Task.Delay(options.PollIntervalMs, cancellationToken);
                    }

                    var cookieContainer = BuildCookieContainer(browserCookies, options.AdditionalCookieUris);
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
                        Timeout = TimeSpan.FromSeconds(options.HttpTimeoutSeconds)
                    };
                    client.DefaultRequestHeaders.TryAddWithoutValidation(
                        "User-Agent",
                        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0");

                    var authenticatedContext = new AuthenticatedHttpClientContext(
                        playwright,
                        browser,
                        context,
                        page,
                        client,
                        handler,
                        cookieContainer,
                        browserPath,
                        browserCookies);

                    try
                    {
                        if (options.AfterReady is not null)
                        {
                            await options.AfterReady(authenticatedContext, cancellationToken);
                            await authenticatedContext.RefreshCookiesFromBrowserAsync(options.AdditionalCookieUris);
                        }

                        return authenticatedContext;
                    }
                    catch
                    {
                        await authenticatedContext.DisposeAsync();
                        throw;
                    }
                }
                catch
                {
                    await context.CloseAsync();
                    throw;
                }
            }
            catch
            {
                await browser.CloseAsync();
                throw;
            }
        }
        catch
        {
            playwright.Dispose();
            throw;
        }
    }

    public static ValueTask<bool> WaitForUrlAsync(
        InteractiveAuthState state,
        CancellationToken cancellationToken,
        params string[] urlKeywords)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (urlKeywords.Length == 0)
        {
            return ValueTask.FromResult(false);
        }

        var currentUrl = state.Page.Url;
        if (string.IsNullOrWhiteSpace(currentUrl))
        {
            return ValueTask.FromResult(false);
        }

        return ValueTask.FromResult(
            urlKeywords.Any(keyword => currentUrl.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
    }

    public static ValueTask<bool> WaitForCookieAsync(
        InteractiveAuthState state,
        CancellationToken cancellationToken,
        string cookieName,
        string? domainKeyword = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(cookieName);

        return ValueTask.FromResult(state.Cookies.Any(cookie =>
            cookie.Name.Equals(cookieName, StringComparison.OrdinalIgnoreCase)
            && (string.IsNullOrWhiteSpace(domainKeyword)
                || cookie.Domain.Contains(domainKeyword, StringComparison.OrdinalIgnoreCase))));
    }

    public static CookieContainer BuildCookieContainer(
        IReadOnlyList<BrowserContextCookiesResult> cookies,
        IEnumerable<Uri>? additionalCookieUris = null)
    {
        var cookieContainer = new CookieContainer();
        var sharedUris = additionalCookieUris?.DistinctBy(static uri => uri.AbsoluteUri).ToArray() ?? [];
        foreach (var cookie in cookies)
        {
            foreach (var uri in GetCandidateUris(cookie, sharedUris))
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

    public static IEnumerable<Uri> GetCandidateUris(
        BrowserContextCookiesResult cookie,
        IEnumerable<Uri>? additionalCookieUris = null)
    {
        var normalizedDomain = cookie.Domain.TrimStart('.');
        if (Uri.TryCreate($"https://{normalizedDomain}/", UriKind.Absolute, out var domainUri))
        {
            yield return domainUri;
        }

        foreach (var parentUri in GetParentDomainUris(normalizedDomain))
        {
            yield return parentUri;
        }

        if (additionalCookieUris is not null)
        {
            foreach (var uri in additionalCookieUris)
            {
                yield return uri;
            }
        }
    }

    public static IEnumerable<Uri> GetParentDomainUris(string hostOrDomain)
    {
        if (string.IsNullOrWhiteSpace(hostOrDomain))
        {
            yield break;
        }

        var normalizedDomain = hostOrDomain.Trim().TrimStart('.');
        if (Uri.TryCreate(normalizedDomain, UriKind.Absolute, out var absoluteUri))
        {
            normalizedDomain = absoluteUri.Host;
        }

        var domainParts = normalizedDomain
            .Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var index = 1; index < domainParts.Length - 1; index++)
        {
            var parentDomain = string.Join('.', domainParts[index..]);
            if (Uri.TryCreate($"https://{parentDomain}/", UriKind.Absolute, out var parentUri))
            {
                yield return parentUri;
            }
        }
    }
}

public sealed class InteractiveAuthOptions
{
    public required string StartUrl { get; init; }
    public required Func<InteractiveAuthState, CancellationToken, ValueTask<bool>> WaitUntil { get; init; }
    public Func<AuthenticatedHttpClientContext, CancellationToken, Task>? AfterReady { get; init; }
    public IEnumerable<Uri>? AdditionalCookieUris { get; init; }
    public string? BrowserExecutablePath { get; init; }
    public bool Headless { get; init; }
    public int NavigationTimeoutMs { get; init; } = ScriptUnifiedAuth.DefaultNavigationTimeoutMs;
    public int HttpTimeoutSeconds { get; init; } = ScriptUnifiedAuth.DefaultHttpTimeoutSeconds;
    public int PollIntervalMs { get; init; } = ScriptUnifiedAuth.DefaultPollIntervalMs;
}

public sealed class InteractiveAuthState
{
    public InteractiveAuthState(
        IBrowserContext browserContext,
        IPage page,
        IReadOnlyList<BrowserContextCookiesResult> cookies)
    {
        BrowserContext = browserContext;
        Page = page;
        Cookies = cookies;
    }

    public IBrowserContext BrowserContext { get; }
    public IPage Page { get; }
    public IReadOnlyList<BrowserContextCookiesResult> Cookies { get; }
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
        Cookies = cookies.ToArray();
    }

    public IPlaywright Playwright { get; }
    public IBrowser Browser { get; }
    public IBrowserContext BrowserContext { get; }
    public IPage Page { get; }
    public HttpClient HttpClient { get; }
    public HttpClientHandler HttpHandler { get; }
    public CookieContainer CookieContainer { get; }
    public string? BrowserPath { get; }
    public IReadOnlyList<BrowserContextCookiesResult> Cookies { get; private set; }

    public async Task RefreshCookiesFromBrowserAsync(IEnumerable<Uri>? additionalCookieUris = null)
    {
        Cookies = await BrowserContext.CookiesAsync();
        var refreshedCookieContainer = ScriptUnifiedAuth.BuildCookieContainer(Cookies, additionalCookieUris);
        var visitedUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var cookie in Cookies)
        {
            foreach (var uri in ScriptUnifiedAuth.GetCandidateUris(cookie, additionalCookieUris))
            {
                if (!visitedUris.Add(uri.AbsoluteUri))
                {
                    continue;
                }

                foreach (System.Net.Cookie existingCookie in CookieContainer.GetCookies(uri))
                {
                    existingCookie.Expired = true;
                }

                foreach (System.Net.Cookie refreshedCookie in refreshedCookieContainer.GetCookies(uri))
                {
                    CookieContainer.Add(uri, refreshedCookie);
                }
            }
        }
    }

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
