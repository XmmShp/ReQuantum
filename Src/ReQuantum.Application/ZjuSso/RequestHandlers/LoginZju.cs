using LoginZju;
using NOF.Application;
using NOF.Contract;
using ReQuantum.Application.ZjuSso.Models;
using ReQuantum.Application.ZjuSso.Services;
using ReQuantum.Contract.ZjuSso;
using System.Text.Json;

namespace ReQuantum.Application.ZjuSso.RequestHandlers;

public class LoginZju(ILoginZjuFactory loginZjuFactory, IZjuAuthAccessor authAccessor) : IRequestHandler<LoginZjuRequest, ZjuLoginInfo>
{
    private const string ServiceHomeUrl = "https://service.zju.edu.cn/";
    private const string LoginInfoUrl = "https://service.zju.edu.cn/_web/portal/api/user/loginInfo.rst?_p=YXM9MiZ0PTUmZD0xMzMmcD0xJmY9MjImbT1OJg__";
    private const string LoginInfoReferer = "https://service.zju.edu.cn/_s2/cs_sy/main.psp";

    public async Task<Result<ZjuLoginInfo>> HandleAsync(LoginZjuRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var auth = loginZjuFactory.CreateAuth(request.Username, request.Password);
            await auth.LoginAsync(cancellationToken);

            var loginInfo = await TryGetLoginInfoAsync(auth, cancellationToken);
            if (loginInfo is null)
            {
                return Result.Fail("400", "登录信息不完整");
            }

            authAccessor.SetSession(auth, loginInfo);
            return loginInfo;
        }
        catch (OperationCanceledException)
        {
            return Result.Fail("400", "登录已取消");
        }
        catch (LoginException ex)
        {
            return Result.Fail("400", $"登录失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"登录失败: {ex.Message}");
        }
    }

    private static async Task<ZjuLoginInfo?> TryGetLoginInfoAsync(IZjuamAuth auth, CancellationToken cancellationToken)
    {
        try
        {
            var serviceCallbackUrl = await auth.LoginServiceAsync(ServiceHomeUrl, cancellationToken);
            using var callbackRequest = new HttpRequestMessage(HttpMethod.Get, serviceCallbackUrl);
            using var callbackResponse = await auth.FetchAsync(callbackRequest, cancellationToken);
            if (!callbackResponse.IsSuccessStatusCode)
            {
                return null;
            }
            using var request = new HttpRequestMessage(HttpMethod.Get, LoginInfoUrl);
            request.Headers.Add("Referer", LoginInfoReferer);
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            request.Headers.Accept.ParseAdd("*/*");

            using var response = await auth.FetchAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(content);
            if (!document.RootElement.TryGetProperty("data", out var data))
            {
                return null;
            }

            var userName = data.TryGetProperty("userName", out var userNameElement)
                ? userNameElement.GetString()
                : null;
            var loginName = data.TryGetProperty("loginName", out var loginNameElement)
                ? loginNameElement.GetString()
                : null;
            var userId = data.TryGetProperty("userId", out var userIdElement)
                ? userIdElement.ToString()
                : null;

            if (string.IsNullOrWhiteSpace(userName)
                || string.IsNullOrWhiteSpace(loginName)
                || string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            return new ZjuLoginInfo(userName, loginName);
        }
        catch
        {
            return null;
        }
    }
}
