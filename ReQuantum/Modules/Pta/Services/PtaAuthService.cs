using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Pta.Services;

public interface IPtaAuthService
{
    [MemberNotNullWhen(true, nameof(Email))]
    bool IsAuthenticated { get; }

    string? Email { get; }

    Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null);

    Task<Result> LoginAsync(string email, string password);

    Result LoginWithSession(string email, string password, string ptaSessionValue);

    void Logout();

    event Action? OnLogin;
    event Action? OnLogout;
}

[AutoInject(Lifetime.Singleton)]
public class PtaAuthService : IPtaAuthService, IDaemonService
{
    private readonly IStorage _storage;

    private const string LoginApiUrl = "https://passport.pintia.cn/api/users/sessions";
    private const string StateKey = "Pta:State";

    private PtaState? _state;

    public PtaAuthService(IStorage storage)
    {
        _storage = storage;
        LoadState();
    }

    [MemberNotNullWhen(true, nameof(_state))]
    public bool IsAuthenticated => _state is not null;

    public string? Email => _state?.Email;

    public event Action? OnLogin;
    public event Action? OnLogout;

    public void Logout()
    {
        OnLogout?.Invoke();
        _state = null;
        SaveState();
    }

    public async Task<Result<RequestClient>> GetAuthenticatedClientAsync(RequestOptions? options = null)
    {
        var result = await ValidOrRefreshTokenAsync();
        if (!result.IsSuccess)
        {
            return Result.Fail(result.Message);
        }

        if (!IsAuthenticated)
        {
            return Result.Fail(nameof(UIText.NotLoggedIn));
        }

        var requestOptions = options ?? new RequestOptions();

        requestOptions.Cookies = requestOptions.Cookies is null
            ? [_state.PTASessionCookie]
            : requestOptions.Cookies.Concat([_state.PTASessionCookie]).ToList();

        // PTA API 需要 Accept 头来返回 JSON 格式（否则返回 Protobuf）
        requestOptions.Headers ??= new Dictionary<string, string>();
        if (!requestOptions.Headers.ContainsKey("Accept"))
        {
            requestOptions.Headers["Accept"] = "application/json, text/plain, */*";
        }

        return RequestClient.Create(requestOptions);
    }

    public async Task<Result> LoginAsync(string email, string password)
    {
        try
        {
            using var client = RequestClient.Create();

            // 使用 JsonObject 构建请求体（避免反射序列化问题）
            var loginJson = new JsonObject
            {
                ["email"] = email,
                ["password"] = password,
                ["rememberMe"] = true
            };

            var jsonContent = new StringContent(
                loginJson.ToJsonString(),
                Encoding.UTF8,
                "application/json"
            );

            // 发送登录请求
            var response = await client.PostAsync(LoginApiUrl, jsonContent);

            // 检查响应状态
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();

                // 尝试解析错误信息
                try
                {
                    var errorJson = JsonDocument.Parse(errorContent);
                    if (errorJson.RootElement.TryGetProperty("error", out var errorProp))
                    {
                        var errorMessage = errorProp.GetProperty("message").GetString();
                        return Result.Fail($"登录失败: {errorMessage}");
                    }
                }
                catch
                {
                    // 无法解析错误信息，返回通用错误
                }

                return Result.Fail($"登录失败 (状态码: {response.StatusCode})");
            }

            // 从响应 Cookie 中获取 PTASession
            var ptaSessionCookie = client.CookieContainer.GetCookies(new Uri(LoginApiUrl))
                .FirstOrDefault(c => c.Name == "PTASession");

            if (ptaSessionCookie is null)
            {
                return Result.Fail("登录失败: 未获取到 PTASession");
            }

            // 保存登录状态
            _state = new PtaState(email, password, ptaSessionCookie);
            SaveState();
            OnLogin?.Invoke();

            return Result.Success("登录成功");
        }
        catch (HttpRequestException ex)
        {
            return Result.Fail($"网络请求失败: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"登录异常: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private async Task<bool> IsTokenValidAsync()
    {
        if (!IsAuthenticated)
        {
            return false;
        }

        try
        {
            using var client = RequestClient.Create(new RequestOptions { Cookies = [_state.PTASessionCookie] });

            // 尝试访问一个需要登录的接口来验证 Session 是否有效
            var response = await client.GetAsync("https://pintia.cn/api/users/profile");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task<Result> ValidOrRefreshTokenAsync()
    {
        if (await IsTokenValidAsync())
        {
            return Result.Success(""); // 当前 Session 有效
        }

        if (!IsAuthenticated)
        {
            return Result.Fail(nameof(UIText.NotLoggedIn)); // 未登录
        }

        // Session 失效，尝试重新登录
        var email = _state.Email;
        var password = _state.Password;

        Logout();

        var loginResult = await LoginAsync(email, password);
        return loginResult.IsSuccess
            ? Result.Success("")
            : Result.Fail("自动重新登录失败");
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

    public Result LoginWithSession(string email, string password, string ptaSessionValue)
    {
        try
        {
            // 创建 Cookie 对象（与 ZjuSso 相同的方式）
            var ptaSessionCookie = new Cookie("PTASession", ptaSessionValue, "/", "pintia.cn");
            _state = new PtaState(email, password, ptaSessionCookie);

            // 添加详细的错误跟踪
            try
            {
                SaveState();
            }
            catch (Exception saveEx)
            {
                return Result.Fail($"保存状态失败: {saveEx.GetType().Name} - {saveEx.Message}\n堆栈: {saveEx.StackTrace}");
            }

            OnLogin?.Invoke();

            return Result.Success("登录成功");
        }
        catch (Exception ex)
        {
            return Result.Fail($"登录异常: {ex.GetType().Name} - {ex.Message}\n堆栈: {ex.StackTrace}");
        }
    }

    public void InitializeDaemon()
    {
        // 初始化时检查登录状态
        _ = ValidOrRefreshTokenAsync();
    }
}
