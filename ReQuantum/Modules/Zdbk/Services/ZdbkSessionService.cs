using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Models;
using ReQuantum.Modules.ZjuSso.Services;
using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkSessionService
{
    Task<Result<RequestClient>> GetAuthenticatedClientAsync();
    ZdbkState? State { get; }
    void UpdateState(ZdbkState state);
    void ClearState();
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkSessionService : IZdbkSessionService, IDaemonService
{
    private readonly IZjuSsoService _zjuSsoService;
    private readonly IStorage _storage;
    private readonly ILogger<ZdbkSessionService> _logger;
    private ZdbkState? _state;

    private const string StateKey = "Zdbk:State";
    private const string BaseUrl = "https://zdbk.zju.edu.cn";
    private const string SsoLoginUrl = "https://zjuam.zju.edu.cn/cas/login";
    private const string SsoRedirectUrl = "/jwglxt/xtgl/login_ssologin.html";
    private const string GetInfoUrl = "/jwglxt/xsxk/zzxkghb_cxZzxkGhbIndex.html";

    public ZdbkState? State => _state;

    public ZdbkSessionService(
        IZjuSsoService zjuSsoService,
        IStorage storage,
        ILogger<ZdbkSessionService> logger)
    {
        _zjuSsoService = zjuSsoService;
        _storage = storage;
        _logger = logger;
        _zjuSsoService.OnLogout += ClearState;
        LoadState();
    }

    public async Task<Result<RequestClient>> GetAuthenticatedClientAsync()
    {
        // 1. Try to use existing state
        if (_state != null)
        {
            // We can try to reuse the session, but for now let's stick to the robust flow of refreshing if needed or just creating a client with existing cookies.
            // However, the original logic always refreshed via SSO.
            // To be safe and consistent with previous behavior, we will proceed to SSO flow.
            // But if we want to optimize, we could check cookie expiration.
        }

        var clientResult = await _zjuSsoService.GetAuthenticatedClientAsync(
            new RequestOptions { AllowRedirects = true });

        if (!clientResult.IsSuccess) return Result.Fail(clientResult.Message);

        try
        {
            var client = clientResult.Value;
            
            var ssoUrl = $"{SsoLoginUrl}?service={Uri.EscapeDataString($"{BaseUrl}{SsoRedirectUrl}")}";
            await client.GetAsync(ssoUrl);

            var allCookies = client.CookieContainer.GetAllCookies();
            var sessionCookie = allCookies.Cast<Cookie>().LastOrDefault(ck => ck.Name == "JSESSIONID" && ck.Domain == "zdbk.zju.edu.cn");
            var route = allCookies.Cast<Cookie>().LastOrDefault(ck => ck.Name == "route");

            if (sessionCookie == null || route == null)
            {
                return Result.Fail("无法获取 Zdbk Session Cookies");
            }

            // Fetch and parse student information
            var infoResponse = await client.GetAsync($"{BaseUrl}{GetInfoUrl}?gnmkdm=N253530");
            if (!infoResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("无法获取学生信息页面，将使用已有状态");
                return Result.Success(RequestClient.Create(new RequestOptions { Cookies = [sessionCookie, route] }));
            }

            var html = await infoResponse.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Parse student information from HTML
            var studentName = doc.DocumentNode.SelectSingleNode("//*[@id='chi']/div/div[1]/h5/span[1]/font/b")?.InnerText?.Trim();
            var studentIdText = doc.DocumentNode.SelectSingleNode("//*[@id='chi']/div/div[1]/h5/span[1]/text()")?.InnerText;
            var studentId = studentIdText != null ? Regex.Match(studentIdText, @"\((\d+)\)").Groups[1].Value : null;
            var grade = doc.DocumentNode.SelectSingleNode("//*[@id='nj']")?.GetAttributeValue("value", null);
            var major = doc.DocumentNode.SelectSingleNode("//*[@id='zydm']")?.GetAttributeValue("value", null);
            var academicYear = doc.DocumentNode.SelectSingleNode("//*[@id='xkxn']")?.InnerText?.Trim();
            var semester = doc.DocumentNode.SelectSingleNode("//*[@id='xq']")?.GetAttributeValue("value", null);

            _logger.LogInformation($"解析学生信息: 姓名={studentName}, 学号={studentId}, 年级={grade}, 专业={major}, 学年={academicYear}, 学期={semester}");

            // Preserve college and administrative class from old state if not found
            var college = _state?.College;
            var adminClass = _state?.AdministrativeClass;

            _state = new ZdbkState(
                sessionCookie,
                route,
                studentId,
                studentName,
                grade,
                major,
                adminClass,
                college,
                academicYear,
                semester
            );
            SaveState();

            return Result.Success(RequestClient.Create(new RequestOptions { Cookies = [sessionCookie, route] }));
        }
        catch (Exception ex)
        {
            return Result.Fail($"SSO认证失败：{ex.Message}");
        }
    }

    public void UpdateState(ZdbkState state)
    {
        _state = state;
        SaveState();
    }

    public void ClearState()
    {
        _state = null;
        _storage.Remove(StateKey);
    }

    private void LoadState() => _storage.TryGetWithEncryption(StateKey, out _state);

    private void SaveState()
    {
        if (_state is null) _storage.Remove(StateKey);
        else _storage.SetWithEncryption(StateKey, _state);
    }
}
