using Microsoft.Playwright;

namespace ReQuantum.ScriptSupport;

public static class ScriptUnifiedAuthZjuCasExtensions
{
    public const string ZjuCasLoginUrl = "https://zjuam.zju.edu.cn/cas/login";

    extension(ScriptUnifiedAuth)
    {
        public static async Task<AuthenticatedHttpClientContext> CreateZjuCasAuthenticatedHttpClientContextAsync(
            string readyHost = "service.zju.edu.cn",
            int navigationTimeoutMs = ScriptUnifiedAuth.DefaultNavigationTimeoutMs,
            int httpTimeoutSeconds = ScriptUnifiedAuth.DefaultHttpTimeoutSeconds,
            int pollIntervalMs = ScriptUnifiedAuth.DefaultPollIntervalMs)
        {
            return await ScriptUnifiedAuth.CreateInteractiveHttpClientContextAsync(
                new InteractiveAuthOptions
                {
                    StartUrl = ZjuCasLoginUrl,
                    NavigationTimeoutMs = navigationTimeoutMs,
                    HttpTimeoutSeconds = httpTimeoutSeconds,
                    PollIntervalMs = pollIntervalMs,
                    WaitUntil = (state, cancellationToken) => new ValueTask<bool>(
                        HasZjuCasReadyState(state.Cookies, state.Page.Url, readyHost)),
                    AdditionalCookieUris = ScriptUnifiedAuth.GetParentDomainUris(readyHost).ToArray()
                });
        }

        public static bool HasZjuCasReadyState(
            IReadOnlyList<BrowserContextCookiesResult> cookies,
            string currentUrl,
            string readyHost)
        {
            if (string.IsNullOrWhiteSpace(currentUrl)
                || !currentUrl.Contains(readyHost, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var hasIPlanet = cookies.Any(static cookie =>
                cookie.Name.Equals("iPlanetDirectoryPro", StringComparison.OrdinalIgnoreCase));
            var hasReadyHostSession = cookies.Any(cookie =>
                    cookie.Name.Equals("JSESSIONID", StringComparison.OrdinalIgnoreCase)
                    && cookie.Domain.Contains(readyHost, StringComparison.OrdinalIgnoreCase))
                || cookies.Any(cookie =>
                    cookie.Name.Equals("route", StringComparison.OrdinalIgnoreCase)
                    && cookie.Domain.Contains(readyHost, StringComparison.OrdinalIgnoreCase));

            return hasIPlanet && hasReadyHostSession;
        }
    }
}
