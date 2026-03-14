using System.Text;
using ReQuantum.ScriptSupport;

const string SsoLoginUrl = "https://zjuam.zju.edu.cn/cas/login?service=https%3A%2F%2Fzdbk.zju.edu.cn%2Fjwglxt%2Fxtgl%2Flogin_ssologin.html";

var commandLineArgs = Environment.GetCommandLineArgs();
var url = commandLineArgs.Skip(1).FirstOrDefault(static arg => Uri.IsWellFormedUriString(arg, UriKind.Absolute))
    ?? "https://zdbk.zju.edu.cn/jwglxt/kbcx/xskbcx_cxXsKb.html?gnmkdm=N253508&su=3230105593";
var formBody = Environment.GetEnvironmentVariable("ZDBK_FORM_BODY")
    ?? "xnm=2025-2026&xqm=2%7C%E6%98%A5&xqmmc=%E6%98%A5&xxqf=0&xsfs=0";

await using var authenticatedContext = await ScriptUnifiedAuth.CreateAuthenticatedHttpClientContextAsync();

using (var ssoLoginRequest = new HttpRequestMessage(HttpMethod.Get, SsoLoginUrl))
{
    ssoLoginRequest.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/145.0.0.0 Safari/537.36 Edg/145.0.0.0");
    using var ssoLoginResponse = await authenticatedContext.HttpClient.SendAsync(ssoLoginRequest);
}

using var request = new HttpRequestMessage(HttpMethod.Post, url)
{
    Content = new StringContent(formBody, Encoding.UTF8, "application/x-www-form-urlencoded")
};

using var response = await authenticatedContext.HttpClient.SendAsync(request);
response.Print();
