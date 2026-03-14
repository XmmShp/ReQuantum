using ReQuantum.ScriptSupport;

const string TargetUrl = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

await using var authenticatedContext = await ScriptUnifiedAuth.CreateAuthenticatedHttpClientContextAsync();

using var request = new HttpRequestMessage(HttpMethod.Get, TargetUrl);
using var response = await authenticatedContext.HttpClient.SendAsync(request);
response.Print();
