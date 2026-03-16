using ReQuantum.ScriptSupport;

const string Url = "https://eta.zju.edu.cn/zftal-xgxt-web/student/xtgl/index/getCurrXn.zf";

await using var authenticatedContext = await ScriptUnifiedAuth.CreateZjuCasAuthenticatedHttpClientContextAsync();

using var request = new HttpRequestMessage(HttpMethod.Get, Url);
using var response = await authenticatedContext.HttpClient.SendAsync(request);
response.Print();
