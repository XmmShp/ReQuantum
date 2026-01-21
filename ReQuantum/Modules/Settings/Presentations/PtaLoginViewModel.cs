using Avalonia.Media.Imaging;
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
    private readonly IPtaPlaywrightService _playwrightService;

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

    [ObservableProperty]
    private Bitmap? _qrCodeBitmap;

    [ObservableProperty]
    private Bitmap? _captchaBitmap;

    [ObservableProperty]
    private string _captchaCode = string.Empty;

    [ObservableProperty]
    private bool _isQrLoginMode;

    [ObservableProperty]
    private bool _isCaptchaVisible;

    public PtaLoginViewModel(IPtaAuthService ptaAuthService, IPtaPlaywrightService playwrightService)
    {
        _ptaAuthService = ptaAuthService;
        _playwrightService = playwrightService;
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
        IsCaptchaVisible = false;
        StatusMessage = "正在登录...";
        DebugInfo = $"[{DateTime.Now:HH:mm:ss}] 开始登录\n邮箱: {Email}\n";

        try
        {
            // 优先尝试普通 API 登录（速度快）
            var result = await _ptaAuthService.LoginAsync(Email, Password);

            if (result.IsSuccess)
            {
                StatusMessage = "登录成功";
                DebugInfo += $"✓ 登录成功\n{result.Message}";
                Password = string.Empty;
            }
            else
            {
                var errorMessage = result.Message.ToString();
                if (errorMessage.Contains("Wrong Captcha") || errorMessage.Contains("captcha"))
                {
                    StatusMessage = "需要验证码，正在启动智能登录...";
                    DebugInfo += $"⚠ 需要验证码，切换到 Playwright 登录";
                    await StartPlaywrightPasswordLoginAsync();
                }
                else
                {
                    StatusMessage = $"登录失败: {errorMessage}";
                    DebugInfo += $"✗ 登录失败\n{errorMessage}";
                    IsLoading = false;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"登录异常: {ex.Message}";
            DebugInfo += $"✗ 异常: {ex.GetType().Name}\n{ex.Message}";
            IsLoading = false;
        }
    }

    private async Task StartPlaywrightPasswordLoginAsync()
    {
        try
        {
            StatusMessage = "正在初始化浏览器环境...";
            var initResult = await _playwrightService.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusMessage = $"初始化失败: {initResult.Message}";
                IsLoading = false;
                return;
            }

            StatusMessage = "正在提交登录信息...";
            var loginResult = await _playwrightService.SubmitPasswordLoginAsync(Email, Password);
            if (!loginResult.IsSuccess)
            {
                StatusMessage = $"提交失败: {loginResult.Message}";
                IsLoading = false;
                return;
            }

            // 检查验证码
            var captchaResult = await _playwrightService.CheckForCaptchaAsync();
            if (captchaResult.IsSuccess && captchaResult.Value != null)
            {
                CaptchaBitmap = new Bitmap(captchaResult.Value);
                IsCaptchaVisible = true;
                NeedsCaptcha = true; // 复用这个属性来控制一些UI显示
                StatusMessage = "请输入图片验证码";
                IsLoading = false; // 暂停 Loading 状态等待用户输入
            }
            else
            {
                // 没有验证码，可能直接成功了，或者失败了
                // 开始等待 Session
                StatusMessage = "等待登录结果...";
                _ = WaitForPlaywrightLoginResultAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"智能登录异常: {ex.Message}";
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SubmitCaptchaAsync()
    {
        if (string.IsNullOrWhiteSpace(CaptchaCode)) return;

        IsLoading = true;
        StatusMessage = "正在提交验证码...";

        var result = await _playwrightService.SubmitCaptchaAsync(CaptchaCode);
        if (result.IsSuccess)
        {
            IsCaptchaVisible = false;
            StatusMessage = "验证码已提交，等待结果...";
            _ = WaitForPlaywrightLoginResultAsync();
        }
        else
        {
            StatusMessage = $"验证码提交失败: {result.Message}";
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SwitchToQrLoginAsync()
    {
        if (IsQrLoginMode) return;

        IsQrLoginMode = true;
        IsLoading = true;
        StatusMessage = "正在获取二维码...";
        
        try
        {
            var initResult = await _playwrightService.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusMessage = $"初始化失败: {initResult.Message}";
                IsLoading = false;
                return;
            }

            var qrResult = await _playwrightService.GetQrCodeAsync();
            
            if (qrResult.IsSuccess)
            {
                QrCodeBitmap = new Bitmap(qrResult.Value);
                StatusMessage = "请使用微信扫描二维码";
                IsLoading = false;
                
                // 开始后台等待登录
                _ = WaitForPlaywrightLoginResultAsync();
            }
            else
            {
                StatusMessage = $"获取二维码失败: {qrResult.Message}";
                IsLoading = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"QR模式异常: {ex.Message}";
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task SwitchToPasswordModeAsync()
    {
        IsQrLoginMode = false;
        IsCaptchaVisible = false;
        QrCodeBitmap = null;
        CaptchaBitmap = null;
        await _playwrightService.CleanupAsync();
        StatusMessage = "已切换到密码登录";
    }

    private async Task WaitForPlaywrightLoginResultAsync()
    {
        var result = await _playwrightService.WaitForLoginSuccessAsync();
        
        IsLoading = false; // 无论成功失败，停止 Loading 
        
        if (result.IsSuccess)
        {
            var session = result.Value;
            // 登录成功
            // 如果是二维码登录，Email/Password 可能是空的，使用占位符
            var email = !string.IsNullOrEmpty(Email) ? Email : "WeChatUser";
            var password = !string.IsNullOrEmpty(Password) ? Password : "QrLogin";
            
            _ptaAuthService.LoginWithSession(email, password, session);
            
            StatusMessage = "登录成功";
            await _playwrightService.CleanupAsync();
        }
        else
        {
            StatusMessage = $"登录超时或失败: {result.Message}";
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
        IsQrLoginMode = false;
        IsCaptchaVisible = false;
        QrCodeBitmap = null;
        CaptchaBitmap = null;
        _playwrightService.CleanupAsync();
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