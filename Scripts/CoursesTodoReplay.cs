using ReQuantum.ScriptSupport;

const string TargetUrl = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

await using var authenticatedContext = await ScriptUnifiedAuth.CreateInteractiveHttpClientContextAsync(
    new InteractiveAuthOptions
    {
        StartUrl = ScriptUnifiedAuthZjuCasExtensions.ZjuCasLoginUrl,
        WaitUntil = (state, cancellationToken) => new ValueTask<bool>(
            ScriptUnifiedAuth.HasZjuCasReadyState(state.Cookies, state.Page.Url, "service.zju.edu.cn")),
        AdditionalCookieUris = ScriptUnifiedAuth.GetParentDomainUris("courses.zju.edu.cn").ToArray(),
        AfterReady = async (context, cancellationToken) =>
        {
            await context.Page.GotoAsync(
                TargetUrl,
                new() { Timeout = ScriptUnifiedAuth.DefaultNavigationTimeoutMs });
        }
    });

using var request = new HttpRequestMessage(HttpMethod.Get, TargetUrl);
using var response = await authenticatedContext.HttpClient.SendAsync(request);
response.Print();
