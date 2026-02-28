using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReQuantum.Application.Models.ZjuSso;
using ReQuantum.Shared.Models;
using ReQuantum.Shared.Services;

namespace ReQuantum.Application.Services.ZjuSso;

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
        LoadState();
    }

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;

    public string? Id => _state?.Id;

    public void Logout()
    {
        OnLogout?.Invoke();
        _state = null;
        SaveState();
    }

    public event Action? OnLogin;
    public event Action? OnLogout;

    public async Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {
        var result = await ValidOrRefreshTokenAsync();
        if (!result.IsSuccess) return Result<RequestClient>.Failure(result.Message);
        if (!IsAuthenticated) return Result<RequestClient>.Failure("未登录");

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
        if (!executionResult.IsSuccess) return Result.Failure(executionResult.Message);
        var execution = executionResult.Value!;

        // Get PubKey
        var pubkeyResult = await GetPubkeyAsync(client);
        if (!pubkeyResult.IsSuccess) return Result.Failure(pubkeyResult.Message);
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
            return Result.Failure("账号可能被锁定");

        var cookieNew = client.CookieContainer.GetCookies(new Uri(LoginUrl))
            .FirstOrDefault(c => c.Name == "iPlanetDirectoryPro");

        if (cookieNew is null)
            return Result.Failure("用户名或密码错误");

        _state = new ZjuSsoState(username, password, cookieNew);
        SaveState();
        OnLogin?.Invoke();
        return Result.Success("登录成功");
    }

    public async Task<Result> OpenBrowserAndWaitForLoginAsync(Action<string>? progressCallback = null, int timeoutSeconds = 300)
    {
        try
        {
            if (_browserLoginProvider is null)
                return Result.Failure("浏览器登录不可用");

            var loginResult = await _browserLoginProvider.OpenBrowserAndWaitForCookieAsync(
                LoginUrl, "iPlanetDirectoryPro", progressCallback, timeoutSeconds);

            if (!loginResult.IsSuccess)
                return Result.Failure($"浏览器登录失败: {loginResult.Message}");

            var result = loginResult.Value!;
            var userId = result.Username ?? "ZJU用户";

            progressCallback?.Invoke("登录成功！");
            return LoginWithSession(userId, result.CookieValue);
        }
        catch (Exception ex)
        {
            return Result.Failure($"浏览器登录失败: {ex.Message}");
        }
    }

    public Result LoginWithSession(string userId, string cookieValue)
    {
        try
        {
            var cookie = new Cookie("iPlanetDirectoryPro", cookieValue, "/", "zju.edu.cn");
            _state = new ZjuSsoState(userId, "", cookie);
            SaveState();
            OnLogin?.Invoke();
            return Result.Success("登录成功");
        }
        catch (Exception ex)
        {
            return Result.Failure($"登录异常: {ex.Message}");
        }
    }

    private async Task<bool> IsTokenValidAsync()
    {
        if (!IsAuthenticated) return false;
        using var client = RequestClient.Create(new RequestOptions { Cookies = [_state.IPlanetDirectoryPro] });
        var response = await client.GetAsync(LoginUrl);
        return response.StatusCode == HttpStatusCode.Redirect;
    }

    private async Task<Result> ValidOrRefreshTokenAsync()
    {
        if (await IsTokenValidAsync()) return Result.Success("登录有效");
        if (!IsAuthenticated) return Result.Failure("未登录");

        var username = _state.Id;
        var password = _state.Password;
        Logout();

        return await LoginAsync(username, password);
    }

    private void LoadState()
    {
        _storage.TryGet(StateKey, out _state);
    }

    private void SaveState()
    {
        if (_state is null) { _storage.Remove(StateKey); return; }
        _storage.Set(StateKey, _state);
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
            result = result.Skip(result.Length - keyLength).ToArray();
        else if (result.Length < keyLength)
            result = new byte[keyLength - result.Length].Concat(result).ToArray();

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
                    return body[start..end];
            }
        }

        return Result<string>.Failure("无法获取execution值");
    }

    private static async Task<Result<(string Modulus, string Exponent)>> GetPubkeyAsync(RequestClient client)
    {
        var json = JsonDocument.Parse(await client.GetStringAsync(PubKeyUrl));
        var mod = json.RootElement.GetProperty("modulus").GetString();
        var exp = json.RootElement.GetProperty("exponent").GetString();

        if (mod is null) return Result<(string, string)>.Failure("无法获取modulus");
        if (exp is null) return Result<(string, string)>.Failure("无法获取exponent");

        return (mod, exp);
    }

    #endregion
}
