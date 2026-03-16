using System.Net;

namespace ReQuantum.Infrastructure.Abstraction;

/// <summary>
/// 自动跟随 HTTP 重定向的 <see cref="DelegatingHandler"/>，保持 <c>Cookie</c> 请求头在跳转链中不丢失。
/// 底层 <see cref="HttpClientHandler"/> 应设置 <c>AllowAutoRedirect = false</c>。
/// </summary>
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
