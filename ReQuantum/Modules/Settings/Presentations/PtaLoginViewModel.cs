using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Services;
using ReQuantum.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton)]
public partial class PtaLoginViewModel : ViewModelBase<PtaLoginView>
{
    private readonly IPtaAuthService _ptaAuthService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoggedIn;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _debugInfo = string.Empty;

    [ObservableProperty]
    private bool _showDebugInfo = true;

    [ObservableProperty]
    private bool _needsCaptcha = false;

    [ObservableProperty]
    private bool _showCookieInput = false;

    [ObservableProperty]
    private string _ptaSessionInput = string.Empty;

    public PtaLoginViewModel(IPtaAuthService ptaAuthService)
    {
        _ptaAuthService = ptaAuthService;
        _isLoggedIn = _ptaAuthService.IsAuthenticated;

        // 订阅登录/登出事件
        _ptaAuthService.OnLogin += HandleLogin;
        _ptaAuthService.OnLogout += HandleLogout;

        if (_isLoggedIn)
        {
            StatusMessage = $"已登录: {_ptaAuthService.Email}";
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading) return;

        if (string.IsNullOrWhiteSpace(Email))
        {
            StatusMessage = "请输入邮箱";
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "请输入密码";
            return;
        }

        IsLoading = true;
        NeedsCaptcha = false;
        StatusMessage = "正在登录...";
        DebugInfo = $"[{DateTime.Now:HH:mm:ss}] 开始登录\n邮箱: {Email}\n";

        try
        {
            var result = await _ptaAuthService.LoginAsync(Email, Password);

            if (result.IsSuccess)
            {
                StatusMessage = "登录成功";
                DebugInfo += $"✓ 登录成功\n{result.Message}";

                // 清空密码
                Password = string.Empty;
            }
            else
            {
                // 检查是否需要验证码
                var errorMessage = result.Message.ToString();
                if (errorMessage.Contains("Wrong Captcha") || errorMessage.Contains("captcha"))
                {
                    NeedsCaptcha = true;
                    StatusMessage = "需要验证码，请使用浏览器登录";
                    DebugInfo += $"⚠ 需要验证码\n点击下方按钮使用浏览器完成登录";
                }
                else
                {
                    StatusMessage = $"登录失败: {errorMessage}";
                    DebugInfo += $"✗ 登录失败\n{errorMessage}";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"登录异常: {ex.Message}";
            DebugInfo += $"✗ 异常: {ex.GetType().Name}\n{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void OpenBrowserLogin()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = "请先输入邮箱和密码";
            return;
        }

        try
        {
            // 打开系统默认浏览器
            var psi = new ProcessStartInfo
            {
                FileName = "https://pintia.cn/auth/login",
                UseShellExecute = true
            };
            Process.Start(psi);

            // 显示 Cookie 输入界面
            ShowCookieInput = true;
            StatusMessage = "请在浏览器中完成登录，然后复制 PTASession Cookie 值";
            DebugInfo = "如何获取 PTASession:\n" +
                        "1. 在浏览器中完成登录（包括验证码）\n" +
                        "2. 按 F12 打开开发者工具\n" +
                        "3. 切换到 Application/存储 标签\n" +
                        "4. 左侧选择 Cookies → https://pintia.cn\n" +
                        "5. 找到 PTASession，复制其值\n" +
                        "6. 粘贴到下方输入框中";
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开浏览器失败: {ex.Message}";
            DebugInfo += $"\n✗ 错误: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SubmitCookie()
    {
        if (string.IsNullOrWhiteSpace(PtaSessionInput))
        {
            StatusMessage = "请输入 PTASession 值";
            return;
        }

        try
        {
            DebugInfo = $"正在使用 Cookie 登录...\nPTASession 长度: {PtaSessionInput.Trim().Length}";

            var result = _ptaAuthService.LoginWithSession(Email, Password, PtaSessionInput.Trim());

            if (result.IsSuccess)
            {
                StatusMessage = "登录成功";
                DebugInfo += "\n✓ Cookie 登录成功";
                ShowCookieInput = false;
                NeedsCaptcha = false;
                Password = string.Empty;
                PtaSessionInput = string.Empty;
            }
            else
            {
                StatusMessage = $"登录失败: {result.Message}";
                DebugInfo += $"\n✗ 登录失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"登录异常: {ex.Message}";
            DebugInfo += $"\n✗ 异常: {ex.GetType().Name}\n{ex.Message}\n{ex.StackTrace}";
        }
    }

    [RelayCommand]
    private void CancelCookieInput()
    {
        ShowCookieInput = false;
        PtaSessionInput = string.Empty;
    }

    [RelayCommand]
    private void Logout()
    {
        _ptaAuthService.Logout();
    }

    private void HandleLogin()
    {
        IsLoggedIn = true;
        StatusMessage = $"已登录: {_ptaAuthService.Email}";
    }

    private void HandleLogout()
    {
        IsLoggedIn = false;
        StatusMessage = "已登出";
        Email = string.Empty;
        Password = string.Empty;
    }

    ~PtaLoginViewModel()
    {
        _ptaAuthService.OnLogin -= HandleLogin;
        _ptaAuthService.OnLogout -= HandleLogout;
    }
}
