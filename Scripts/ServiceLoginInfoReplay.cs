using ReQuantum.ScriptSupport;

const string Url = "https://service.zju.edu.cn/_web/portal/api/user/loginInfo.rst?_p=YXM9MiZ0PTUmZD0xMzMmcD0xJmY9MjImbT1OJg__";
await using var authenticatedContext = await ScriptUnifiedAuth.CreateAuthenticatedHttpClientContextAsync();

using var request = new HttpRequestMessage(HttpMethod.Get, Url);

using var response = await authenticatedContext.HttpClient.SendAsync(request);
response.Print();
