using System.Text;
using ReQuantum.ScriptSupport;

const string EtaSwitchRoleUrl = "https://eta.zju.edu.cn/zftal-xgxt-web/teacher/xtgl/login/switchRole.zf";

var xnxq = ScriptHttp.RequireEnvironmentVariable("ETA_XNXQ");
var url = $"https://eta.zju.edu.cn/zftal-xgxt-web/student/xtgl/index/getTableKcb.zf?xnxq={Uri.EscapeDataString(xnxq)}";

await using var authenticatedContext = await ScriptUnifiedAuth.CreateZjuCasAuthenticatedHttpClientContextAsync();

using (var switchRoleRequest = new HttpRequestMessage(HttpMethod.Post, EtaSwitchRoleUrl))
{
    using var switchRoleResponse = await authenticatedContext.HttpClient.SendAsync(switchRoleRequest);
}

using var request = new HttpRequestMessage(HttpMethod.Get, url);
using var response = await authenticatedContext.HttpClient.SendAsync(request);
response.Print();