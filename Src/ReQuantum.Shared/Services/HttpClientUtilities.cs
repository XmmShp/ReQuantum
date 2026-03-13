using System.Net;

namespace ReQuantum.Shared.Services;

public static class HttpClientUtilities
{
    public static HttpClient Create(RequestOptions? options = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            AllowAutoRedirect = options?.AllowRedirects ?? true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli
        };

        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? 100)
        };

        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");

        if (options?.Headers is not null)
        {
            foreach (var header in options.Headers)
            {
                client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }

        return client;
    }

    public static void ApplyCookies(HttpRequestMessage request, IEnumerable<Cookie>? cookies)
    {
        var cookieHeader = BuildCookieHeader(cookies);
        if (!string.IsNullOrWhiteSpace(cookieHeader))
        {
            request.Headers.Remove("Cookie");
            request.Headers.Add("Cookie", cookieHeader);
        }
    }

    public static string BuildCookieHeader(IEnumerable<Cookie>? cookies)
    {
        if (cookies is null)
        {
            return string.Empty;
        }

        return string.Join("; ", cookies
            .Where(static cookie => !string.IsNullOrWhiteSpace(cookie.Name))
            .Select(static cookie => $"{cookie.Name}={cookie.Value}"));
    }

    public static List<Cookie> ReadResponseCookies(HttpResponseMessage response)
    {
        var requestUri = response.RequestMessage?.RequestUri;
        var defaultDomain = requestUri?.Host ?? string.Empty;
        var cookies = new List<Cookie>();

        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return cookies;
        }

        foreach (var header in values)
        {
            var segments = header.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0)
            {
                continue;
            }

            var nameValue = segments[0].Split('=', 2);
            if (nameValue.Length != 2)
            {
                continue;
            }

            var name = nameValue[0].Trim();
            var value = nameValue[1].Trim();
            var domain = defaultDomain;
            var path = "/";

            foreach (var segment in segments.Skip(1))
            {
                var parts = segment.Split('=', 2);
                if (parts.Length != 2)
                {
                    continue;
                }

                if (parts[0].Equals("Domain", StringComparison.OrdinalIgnoreCase))
                {
                    domain = parts[1].Trim().TrimStart('.');
                }
                else if (parts[0].Equals("Path", StringComparison.OrdinalIgnoreCase))
                {
                    path = parts[1].Trim();
                }
            }

            cookies.Add(new Cookie(name, value, path, domain));
        }

        return cookies;
    }

    public static List<Cookie> MergeCookies(IEnumerable<Cookie>? existing, IEnumerable<Cookie>? incoming)
    {
        var merged = new List<Cookie>();

        if (existing is not null)
        {
            merged.AddRange(existing);
        }

        if (incoming is null)
        {
            return merged;
        }

        foreach (var cookie in incoming)
        {
            var index = merged.FindIndex(current =>
                current.Name.Equals(cookie.Name, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(current.Domain, cookie.Domain, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(current.Path, cookie.Path, StringComparison.OrdinalIgnoreCase));

            if (index >= 0)
            {
                merged[index] = cookie;
            }
            else
            {
                merged.Add(cookie);
            }
        }

        return merged;
    }

    public static async Task<HttpResponseMessage> GetWithCookieTrackingAsync(HttpClient client, string url, IList<Cookie> cookies, int maxRedirects = 10)
    {
        Uri? currentUri = new(url, UriKind.Absolute);

        for (var i = 0; i <= maxRedirects; i++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, currentUri);
            ApplyCookies(request, cookies);

            var response = await client.SendAsync(request);
            var responseCookies = ReadResponseCookies(response);
            var mergedCookies = MergeCookies(cookies, responseCookies);
            cookies.Clear();
            foreach (var cookie in mergedCookies)
            {
                cookies.Add(cookie);
            }

            if ((int)response.StatusCode is < 300 or >= 400 || response.Headers.Location is null)
            {
                return response;
            }

            currentUri = response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location
                : new Uri(currentUri, response.Headers.Location);

            response.Dispose();
        }

        throw new InvalidOperationException("Too many redirects.");
    }
}
