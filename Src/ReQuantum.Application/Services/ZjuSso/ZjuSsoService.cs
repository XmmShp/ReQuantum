using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Models.ZjuSso;
using ReQuantum.Shared.Services;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace ReQuantum.Application.Services.ZjuSso;

[AutoInject(Lifetime.Singleton)]
public class ZjuSsoService : IZjuSsoService
{
    private readonly IStorage _storage;
    private readonly ILogger<ZjuSsoService> _logger;
    private readonly IBrowserLoginProvider? _browserLoginProvider;
    private const string LoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string PubKeyUrl = "https://zjuam.zju.edu.cn/cas/v2/getPubKey";
    private const string StateKey = "ZjuSso:State";

    private ZjuSsoState? _state;

    public ZjuSsoService(IStorage storage, ILogger<ZjuSsoService> logger, IBrowserLoginProvider? browserLoginProvider = null)
    {
        _storage = storage;
        _logger = logger;
        _browserLoginProvider = browserLoginProvider;
    }

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;

    public string? Id => _state?.Id;

    public void Logout()
    {
        OnLogout?.Invoke();
        _state = null;
        _ = SaveStateAsync();
    }

    public event Action? OnLogin;
    public event Action? OnLogout;

    public async Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {
        await LoadStateAsync();

        var result = await ValidOrRefreshTokenAsync();
        if (!result.IsSuccess)
        {
            return Result.Fail("400", result.Message);
        }

        if (!IsAuthenticated)
        {
            return Result.Fail("400", "未登录");
        }

        var requestOptions = options ?? new RequestOptions();
        requestOptions.Cookies = requestOptions.Cookies is null
            ? [_state.IPlanetDirectoryPro]
            : requestOptions.Cookies.Concat([_state.IPlanetDirectoryPro]).ToList();

        return RequestClient.Create(requestOptions);
    }

    public async Task<Result> LoginAsync(string username, string password)
    {
        using var client = RequestClient.Create();

        // Get Execution
        var executionResult = await GetExecutionAsync(client);
        if (!executionResult.IsSuccess)
        {
            return Result.Fail("400", executionResult.Message);
        }

        var execution = executionResult.Value!;

        // Get PubKey
        var pubkeyResult = await GetPubkeyAsync(client);
        if (!pubkeyResult.IsSuccess)
        {
            return Result.Fail("400", pubkeyResult.Message);
        }

        var (modulus, exponent) = pubkeyResult.Value!;

        // 使用RSA公钥加密密码
        var encryptedPass = EncryptRsa(password, modulus, exponent);
        var encryptedPassStr = Convert.ToHexString(encryptedPass).ToLower().TrimStart('0');

        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", username },
            { "password", encryptedPassStr },
            { "authcode", "" },
            { "execution", execution },
            { "_eventId", "submit" }
        });

        var response = await client.PostAsync(LoginUrl, formContent);
        if (!response.IsSuccessStatusCode)
        {
            return Result.Fail("400", "账号可能被锁定");
        }

        var cookieNew = client.CookieContainer.GetCookies(new Uri(LoginUrl))
            .FirstOrDefault(c => c.Name == "iPlanetDirectoryPro");

        if (cookieNew is null)
        {
            return Result.Fail("400", "用户名或密码错误");
        }

        _state = new ZjuSsoState(username, password, cookieNew);
        await SaveStateAsync();
        OnLogin?.Invoke();
        return Result.Success();
    }

    public async Task<Result> OpenBrowserAndWaitForLoginAsync(Action<string>? progressCallback = null, int timeoutSeconds = 300)
    {
        try
        {
            if (_browserLoginProvider is null)
            {
                return Result.Fail("400", "浏览器登录不可用");
            }

            var loginResult = await _browserLoginProvider.OpenBrowserAndWaitForCookieAsync(
                LoginUrl, "iPlanetDirectoryPro", progressCallback, timeoutSeconds);

            if (!loginResult.IsSuccess)
            {
                return Result.Fail("400", $"浏览器登录失败: {loginResult.Message}");
            }

            var result = loginResult.Value!;
            var userId = result.Username ?? "ZJU用户";

            progressCallback?.Invoke("登录成功！");
            return LoginWithSession(userId, result.CookieValue);
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"浏览器登录失败: {ex.Message}");
        }
    }

    public Result LoginWithSession(string userId, string cookieValue)
    {
        try
        {
            var cookie = new Cookie("iPlanetDirectoryPro", cookieValue, "/", "zju.edu.cn");
            _state = new ZjuSsoState(userId, "", cookie);
            _ = SaveStateAsync();
            OnLogin?.Invoke();
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Fail("400", $"登录异常: {ex.Message}");
        }
    }

    private async Task<bool> IsTokenValidAsync()
    {
        await LoadStateAsync();

        if (!IsAuthenticated)
        {
            return false;
        }

        using var client = RequestClient.Create(new RequestOptions { Cookies = [_state.IPlanetDirectoryPro] });
        var response = await client.GetAsync(LoginUrl);
        return response.StatusCode == HttpStatusCode.Redirect;
    }

    private async Task<Result> ValidOrRefreshTokenAsync()
    {
        await LoadStateAsync();

        if (await IsTokenValidAsync())
        {
            return Result.Success();
        }

        if (!IsAuthenticated)
        {
            return Result.Fail("400", "未登录");
        }

        var username = _state.Id;
        var password = _state.Password;
        Logout();

        return await LoginAsync(username, password);
    }

    private async ValueTask LoadStateAsync()
    {
        if (_state is not null)
        {
            return;
        }

        var state = await _storage.TryGetAsync<ZjuSsoState>(StateKey);
        _state = state.ValueOr((ZjuSsoState?)null);
    }

    private async ValueTask SaveStateAsync()
    {
        if (_state is null)
        {
            await _storage.RemoveAsync(StateKey);
            return;
        }

        await _storage.SetAsync(StateKey, _state);
    }

    #region Static helpers

    private static byte[] EncryptRsa(string message, string modulus, string exponent)
    {
        var n = BigInteger.Parse("00" + modulus, NumberStyles.HexNumber);
        var e = BigInteger.Parse(exponent, NumberStyles.HexNumber);
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var m = new BigInteger(Enumerable.Reverse(messageBytes).Concat(new byte[] { 0 }).ToArray());
        var c = BigInteger.ModPow(m, e, n);
        var keyLength = (n.ToString("X").Length + 1) / 2;
        var result = Enumerable.Reverse(c.ToByteArray()).ToArray();

        if (result.Length > keyLength)
        {
            result = result.Skip(result.Length - keyLength).ToArray();
        }
        else if (result.Length < keyLength)
        {
            result = new byte[keyLength - result.Length].Concat(result).ToArray();
        }

        return result;
    }

    private static async Task<Result<string>> GetExecutionAsync(RequestClient client)
    {
        const int maxRetryCount = 3;
        for (var i = 0; i < maxRetryCount; i++)
        {
            var res = await client.GetAsync(LoginUrl);
            var body = await res.Content.ReadAsStringAsync();

            // 简单解析 execution 值
            var marker = "name=\"execution\" value=\"";
            var idx = body.IndexOf(marker, StringComparison.Ordinal);
            if (idx >= 0)
            {
                var start = idx + marker.Length;
                var end = body.IndexOf('"', start);
                if (end > start)
                {
                    return body[start..end];
                }
            }
        }

        return Result.Fail("400", "无法获取execution值");
    }

    private static async Task<Result<(string Modulus, string Exponent)>> GetPubkeyAsync(RequestClient client)
    {
        var json = JsonDocument.Parse(await client.GetStringAsync(PubKeyUrl));
        var mod = json.RootElement.GetProperty("modulus").GetString();
        var exp = json.RootElement.GetProperty("exponent").GetString();

        if (mod is null)
        {
            return Result.Fail("400", "无法获取modulus");
        }

        if (exp is null)
        {
            return Result.Fail("400", "无法获取exponent");
        }

        return (mod, exp);
    }

    #endregion
}
