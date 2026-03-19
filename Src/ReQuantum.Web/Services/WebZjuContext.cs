using LoginZju;
using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.ZjuSso.Abstractions;
using ReQuantum.Application.ZjuSso.Models;
using ReQuantum.Application.ZjuSso.Services;
using System.Text.Json;

namespace ReQuantum.Web.Services;

public class WebZjuContext : ZjuContext
{
    private const string LoginInfoUrl = "https://service.zju.edu.cn/_web/portal/api/user/loginInfo.rst?_p=YXM9MiZ0PTUmZD0xMzMmcD0xJmY9MjImbT1OJg__";
    private const string LoginInfoReferer = "https://service.zju.edu.cn/_s2/cs_sy/main.psp";
    private readonly IReadOnlyList<IZjuLoginAfterReadyHandler> _afterReadyHandlers;
    private readonly WebHttpContext _httpContext;
    private readonly ILoginZjuFactory _loginZjuFactory;
    private readonly ZjuamAuthHolder _authHolder;

    public WebZjuContext(
        IStorage storage,
        IEncryptor encryptor,
        WebHttpContext httpContext,
        IEnumerable<IZjuLoginAfterReadyHandler> afterReadyHandlers,
        ILoginZjuFactory loginZjuFactory,
        ZjuamAuthHolder authHolder) : base(storage, encryptor)
    {
        _httpContext = httpContext;
        _afterReadyHandlers = afterReadyHandlers.ToArray();
        _loginZjuFactory = loginZjuFactory;
        _authHolder = authHolder;
    }

    public override async Task<Result> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        try
        {
            var auth = _loginZjuFactory.CreateAuth(username, password);
            await auth.LoginAsync(cancellationToken);
            _authHolder.SetAuth(auth);

            var loginInfo = await TryGetLoginInfoAsync(auth, cancellationToken);
            var afterReadyResult = await RunAfterReadyHandlersAsync(auth, cancellationToken);
            if (!afterReadyResult.IsSuccess)
            {
                return afterReadyResult;
            }

            if (loginInfo is null)
            {
                return Result.Fail("400", "登录信息不完整");
            }

            var result = await SetLoginInfoAsync(loginInfo);
            if (result.IsSuccess)
            {
                await PersistCredentialsAsync(username, password);
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

    private async Task<Result> RunAfterReadyHandlersAsync(IZjuamAuth auth, CancellationToken cancellationToken)
    {
        var context = new AuthAfterReadyContext(auth);
        foreach (var handler in _afterReadyHandlers)
        {
            var result = await handler.OnAfterReadyAsync(context, cancellationToken);
            if (!result.IsSuccess)
            {
                return result;
            }
        }

        return Result.Success();
    }

    public new void Logout()
    {
        _httpContext.ClearCookies();
        _authHolder.SetAuth(null);
        base.Logout();
    }

    private static async Task<ZjuLoginInfo?> TryGetLoginInfoAsync(IZjuamAuth auth, CancellationToken cancellationToken)
    {
        try
        {
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

    private sealed class AuthAfterReadyContext : IZjuLoginAfterReadyContext
    {
        private readonly IZjuamAuth _auth;

        public AuthAfterReadyContext(IZjuamAuth auth)
        {
            _auth = auth;
        }

        public async Task<Result> VisitAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var _ = await _auth.FetchAsync(request, cancellationToken);
                return Result.Success();
            }
            catch (OperationCanceledException)
            {
                return Result.Fail("400", "登录后回调已取消");
            }
            catch (Exception ex)
            {
                return Result.Fail("400", $"登录后访问页面失败: {ex.Message}");
            }
        }
    }

}
