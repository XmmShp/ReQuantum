using System.Net;

namespace ReQuantum.ScriptSupport;

public static class ScriptHttp
{
    public static string RequireEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        Console.Error.WriteLine($"Missing required environment variable: {name}");
        Environment.Exit(1);
        return string.Empty;
    }

    public static HttpClientHandler CreateHandler()
    {
        return new HttpClientHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            UseCookies = false,
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
    }

    public static IReadOnlyList<string> TryGetHeaderValues(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var headerValues))
        {
            return headerValues.ToList();
        }

        if (response.Content.Headers.TryGetValues(headerName, out var contentHeaderValues))
        {
            return contentHeaderValues.ToList();
        }

        return Array.Empty<string>();
    }

    public static void Print(this HttpResponseMessage response)
    {
        Console.WriteLine($"Status: {(int)response.StatusCode} {response.ReasonPhrase}");
        Console.WriteLine($"RequestUri: {response.RequestMessage?.RequestUri}");

        if (response.Headers.Location is not null)
        {
            Console.WriteLine($"Location: {response.Headers.Location}");
        }

        foreach (var value in TryGetHeaderValues(response, "content-type"))
        {
            Console.WriteLine($"content-type: {value}");
        }

        foreach (var value in TryGetHeaderValues(response, "set-cookie"))
        {
            Console.WriteLine($"set-cookie: {value}");
        }

        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        if (!string.IsNullOrWhiteSpace(body))
        {
            Console.WriteLine("Body:");
            Console.WriteLine(body);
        }

        Console.WriteLine();
    }
}
