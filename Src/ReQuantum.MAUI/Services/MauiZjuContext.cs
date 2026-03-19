using LoginZju;
using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.ZjuSso.Models;
using ReQuantum.Application.ZjuSso.Services;
using System.Text.Json;

namespace ReQuantum.Infrastructure.Services;

public class MauiZjuContext : ZjuContext
{
    private const string ServiceHomeUrl = "https://service.zju.edu.cn/";
    private const string LoginInfoUrl = "https://service.zju.edu.cn/_web/portal/api/user/loginInfo.rst?_p=YXM9MiZ0PTUmZD0xMzMmcD0xJmY9MjImbT1OJg__";
    private const string LoginInfoReferer = "https://service.zju.edu.cn/_s2/cs_sy/main.psp";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoginZjuFactory _loginZjuFactory;
    private readonly ZjuamAuthHolder _authHolder;

    public MauiZjuContext(
        IStorage storage,
        IEncryptor encryptor,
        IServiceScopeFactory scopeFactory,
        ILoginZjuFactory loginZjuFactory,
        ZjuamAuthHolder authHolder) : base(storage, encryptor)
    {
        _scopeFactory = scopeFactory;
        _loginZjuFactory = loginZjuFactory;
        _authHolder = authHolder;
        OnLogout += () =>
        {
            _authHolder.SetAuth(null);
        };
    }

    public override async Task<Result> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var auth = _loginZjuFactory.CreateAuth(username, password);
            await auth.LoginAsync(cancellationToken);
            _authHolder.SetAuth(auth);

            var loginInfo = await TryGetLoginInfoAsync(auth, cancellationToken);

            if (loginInfo is null)
            {
                return Result.Fail("400", "登录信息不完整");
            }

            var result = await SetLoginInfoAsync(loginInfo, cancellationToken);
            if (result.IsSuccess)
            {
                await PersistCredentialsAsync(username, password);
                _ = Task.Run(EstablishCoursesSessionAndSyncAsync, CancellationToken.None);
            }

            return result;
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

    private async Task EstablishCoursesSessionAndSyncAsync()
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var tasks = scope.ServiceProvider.GetServices<IBackgroundTask>();
        foreach (var task in tasks)
        {
            await task.ExecuteAsync();
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

            cancellationToken.ThrowIfCancellationRequested();
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
                && string.IsNullOrWhiteSpace(loginName)
                && string.IsNullOrWhiteSpace(userId))
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
