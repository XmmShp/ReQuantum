using HtmlAgilityPack;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.PintiaLogin.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ReQuantum.Modules.PintiaLogin.Services;

public interface IPintiaService
{
    [MemberNotNullWhen(true, nameof(Id))]
    bool IsAuthenticated { get; }
    string? Id { get; }
    Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null);
    Task<Result> LoginAsync(string username, string password);
    void Logout();
    event Action? OnLogin;
    event Action? OnLogout;
}

[AutoInject(Lifetime.Singleton)]
public class PintiaService : IPintiaService, IDaemonService
{
    private readonly IStorage _storage;
    private const string LoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string PubKeyUrl = "https://zjuam.zju.edu.cn/cas/v2/getPubKey";
    private readonly HttpClient _httpClient;

    private const string StateKey = "ZjuSso:State";
    public PintiaService(IStorage storage, HttpClient httpClient)
    {
        _storage = storage;
        _httpClient = httpClient;
        LoadState();
    }
    private PintiaState? _state;

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;
    public string? Id => _state?.Id;


    public void Logout()
    {
        _state = null;
        SaveState();
        OnLogout?.Invoke();
    }

    public event Action? OnLogin;
    public event Action? OnLogout;

    public Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {

        if (!IsAuthenticated)
        {
            return Task.FromResult<Result<RequestClient>>(Result.Fail(nameof(UIText.NotLoggedIn)));
        }

        var requestOptions = options ?? new RequestOptions();
        requestOptions.Cookies = requestOptions.Cookies is null
            ? new List<Cookie> { _state.IPlanetDirectoryPro }
            : requestOptions.Cookies.Concat(new[] { _state.IPlanetDirectoryPro }).ToList();

        return Task.FromResult(Result.Success<RequestClient>(RequestClient.Create(requestOptions)));
    }

    public async Task<Result> LoginAsync(string username, string password)
    {
        using var client = RequestClient.Create();

        // Get Execution
        var executionResult = await GetExecutionAsync(client);

        if (!executionResult.IsSuccess)
        {
            return Result.Fail(executionResult.Message);
        }

        var execution = executionResult.Value;

        // Get PubKey
        var pubkeyResult = await GetPubkeyAsync(client);

        if (!pubkeyResult.IsSuccess)
        {
            return Result.Fail(pubkeyResult.Message);
        }

        var (modulus, exponent) = pubkeyResult.Value;

        // 使用RSA公钥加密密码
        var encryptedPass = EncryptRsa(password, modulus, exponent);
        var encryptedPassStr = Convert.ToHexString(encryptedPass).ToLower().TrimStart('0');

        // 构建登录表单数据
        var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "username", username },
            { "password", encryptedPassStr },
            { "authcode", "" },
            { "execution", execution },
            { "_eventId", "submit" }
        });

        // 提交登录表单
        var response = await client.PostAsync(LoginUrl, formContent);

        if (!response.IsSuccessStatusCode)
        {
            return Result.Fail(nameof(UIText.AccountMayBeLocked));
        }

        // 检查登录状态
        var cookieNew = client.CookieContainer.GetCookies(new Uri(LoginUrl))
            .FirstOrDefault(c => c.Name == "iPlanetDirectoryPro");

        if (cookieNew is null)
        {
            return Result.Fail(nameof(UIText.IncorrectUsernameOrPassword));
        }

        _state = new PintiaState(username, password, cookieNew);
        SaveState();
        OnLogin?.Invoke();
        return Result.Success(nameof(UIText.LoginSuccessful));

        #region LocalFunctions
        static byte[] EncryptRsa(string message, string modulus, string exponent)
        {
            // 将16进制字符串转换为BigInteger
            var n = BigInteger.Parse("00" + modulus, NumberStyles.HexNumber);
            var e = BigInteger.Parse(exponent, NumberStyles.HexNumber);

            // 将消息转换为字节数组
            var messageBytes = Encoding.UTF8.GetBytes(message);

            // 将消息字节转换为BigInteger
            var m = new BigInteger(Enumerable.Reverse(messageBytes).Concat(new byte[] { 0 }).ToArray());

            // 执行RSA加密: c = m^e mod n
            var c = BigInteger.ModPow(m, e, n);

            // 计算密钥长度（以字节为单位）
            var keyLength = (n.ToString("X").Length + 1) / 2;

            // 将加密后的BigInteger转换为字节数组
            var result = Enumerable.Reverse(c.ToByteArray()).ToArray();

            // 确保结果长度正确
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

        static async Task<Result<string>> GetExecutionAsync(RequestClient requestClient)
        {
            var s = string.Empty;

            const int maxRetryCount = 3;

            for (var i = 0; i < maxRetryCount; i++)
            {
                var res = await requestClient.GetAsync(LoginUrl);
                var responseBody = await res.Content.ReadAsStringAsync();

                var doc = new HtmlDocument();
                doc.LoadHtml(responseBody);
                var executionNode = doc.DocumentNode.SelectSingleNode("//input[@name='execution']");
                s = executionNode.GetAttributeValue("value", "");

                if (!string.IsNullOrEmpty(s))
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(s))
            {
                return Result.Fail(nameof(UIText.FailedToGetExecutionValue));
            }

            return s;
        }

        static async Task<Result<(string Modulus, string Exponent)>> GetPubkeyAsync(RequestClient requestClient)
        {
            // 获取公钥
            var pubKeyJson = JsonDocument.Parse(await requestClient.GetStringAsync(PubKeyUrl));
            var mod = pubKeyJson.RootElement.GetProperty("modulus").GetString();
            var exp = pubKeyJson.RootElement.GetProperty("exponent").GetString();

            if (mod is null)
            {
                return Result.Fail(nameof(UIText.FailedToGetModulus));
            }

            if (exp is null)
            {
                return Result.Fail(nameof(UIText.FailedToGetExponent));
            }

            return (mod, exp);
        }
        #endregion
    }
    private async Task<bool> IsTokenValidAsync()
    {
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
        if (await IsTokenValidAsync())
        {
            return Result.Success(nameof(UIText.LoginSuccessful));
        } // 当前Cookie有效

        if (!IsAuthenticated)
        {
            return Result.Fail(nameof(UIText.NotLoggedIn));
        } // 未登录

        var username = _state.Id;
        var password = _state.Password;

        Logout();

        using var client = RequestClient.Create();

        var loginResult = await LoginAsync(username, password);
        return loginResult.IsSuccess
            ? Result.Success(nameof(UIText.LoginSuccessful))
            : Result.Fail(nameof(UIText.IncorrectUsernameOrPassword));
    }

    private void LoadState()
    {
        _storage.TryGetWithEncryption(StateKey, out _state);
    }

    private void SaveState()
    {
        if (_state is null)
        {
            _storage.Remove(StateKey);
            return;
        }

        _storage.SetWithEncryption(StateKey, _state);
    }


}


