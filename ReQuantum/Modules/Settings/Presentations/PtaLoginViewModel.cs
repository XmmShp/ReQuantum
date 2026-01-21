using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
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

        // 默认使用二维码登录模式
        _isQrLoginMode = true;

        // 订阅登录/登出事件
        _ptaAuthService.OnLogin += HandleLogin;
        _ptaAuthService.OnLogout += HandleLogout;

        if (_isLoggedIn)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoggedInAs)], _ptaAuthService.Email);
        }
        else
        {
            // 未登录时自动加载二维码
            _ = InitializeQrCodeAsync();
        }
    }

    private async Task InitializeQrCodeAsync()
    {
        IsLoading = true;
        StatusMessage = Localizer[nameof(UIText.PtaQrCodeLoading)];

        try
        {
            var initResult = await _playwrightService.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaInitFailed)], initResult.Message);
                IsLoading = false;
                return;
            }

            var qrResult = await _playwrightService.GetQrCodeAsync();

            if (qrResult.IsSuccess)
            {
                QrCodeBitmap = new Bitmap(qrResult.Value);
                StatusMessage = Localizer[nameof(UIText.PtaScanQrCode)];
                IsLoading = false;

                // 开始后台等待登录
                _ = WaitForPlaywrightLoginResultAsync();
            }
            else
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaGetQrCodeFailed)], qrResult.Message);
                IsLoading = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoadQrCodeException)], ex.Message);
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading) return;

        if (string.IsNullOrWhiteSpace(Email))
        {
            StatusMessage = Localizer[nameof(UIText.PtaPleaseEnterEmail)];
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = Localizer[nameof(UIText.PleaseEnterPassword)];
            return;
        }

        IsLoading = true;
        NeedsCaptcha = false;
        IsCaptchaVisible = false;
        StatusMessage = Localizer[nameof(UIText.PtaLoggingIn)];
        DebugInfo = $"[{DateTime.Now:HH:mm:ss}] 开始登录\n邮箱: {Email}\n";

        try
        {
            // 优先尝试普通 API 登录（速度快）
            var result = await _ptaAuthService.LoginAsync(Email, Password);

            if (result.IsSuccess)
            {
                StatusMessage = Localizer[nameof(UIText.PtaLoginSuccess)];
                DebugInfo += $"✓ 登录成功\n{result.Message}";
                Password = string.Empty;
            }
            else
            {
                var errorMessage = result.Message.ToString();
                if (errorMessage.Contains("Wrong Captcha") || errorMessage.Contains("captcha"))
                {
                    StatusMessage = Localizer[nameof(UIText.PtaNeedCaptchaStartingPlaywright)];
                    DebugInfo += $"⚠ 需要验证码，切换到 Playwright 登录";
                    await StartPlaywrightPasswordLoginAsync();
                }
                else
                {
                    StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoginFailedWithReason)], errorMessage);
                    DebugInfo += $"✗ 登录失败\n{errorMessage}";
                    IsLoading = false;
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoginException)], ex.Message);
            DebugInfo += $"✗ 异常: {ex.GetType().Name}\n{ex.Message}";
            IsLoading = false;
        }
    }

    private async Task StartPlaywrightPasswordLoginAsync()
    {
        try
        {
            StatusMessage = Localizer[nameof(UIText.PtaInitializingBrowser)];
            var initResult = await _playwrightService.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaInitFailed)], initResult.Message);
                IsLoading = false;
                return;
            }

            StatusMessage = Localizer[nameof(UIText.PtaSubmittingLogin)];
            var loginResult = await _playwrightService.SubmitPasswordLoginAsync(Email, Password);
            if (!loginResult.IsSuccess)
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaSubmitFailed)], loginResult.Message);
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
                StatusMessage = Localizer[nameof(UIText.PtaPleaseEnterCaptcha)];
                IsLoading = false; // 暂停 Loading 状态等待用户输入
            }
            else
            {
                // 没有验证码，可能直接成功了，或者失败了
                // 开始等待 Session
                StatusMessage = Localizer[nameof(UIText.PtaWaitingForResult)];
                _ = WaitForPlaywrightLoginResultAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaPlaywrightLoginException)], ex.Message);
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SubmitCaptchaAsync()
    {
        if (string.IsNullOrWhiteSpace(CaptchaCode)) return;

        IsLoading = true;
        StatusMessage = Localizer[nameof(UIText.PtaSubmittingCaptcha)];

        var result = await _playwrightService.SubmitCaptchaAsync(CaptchaCode);
        if (result.IsSuccess)
        {
            IsCaptchaVisible = false;
            StatusMessage = Localizer[nameof(UIText.PtaCaptchaSubmittedWaiting)];
            _ = WaitForPlaywrightLoginResultAsync();
        }
        else
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaCaptchaSubmitFailed)], result.Message);
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SwitchToQrLoginAsync()
    {
        if (IsQrLoginMode) return;

        IsQrLoginMode = true;
        IsLoading = true;
        StatusMessage = Localizer[nameof(UIText.PtaQrCodeLoading)];

        try
        {
            var initResult = await _playwrightService.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaInitFailed)], initResult.Message);
                IsLoading = false;
                return;
            }

            var qrResult = await _playwrightService.GetQrCodeAsync();

            if (qrResult.IsSuccess)
            {
                QrCodeBitmap = new Bitmap(qrResult.Value);
                StatusMessage = Localizer[nameof(UIText.PtaScanQrCode)];
                IsLoading = false;

                // 开始后台等待登录
                _ = WaitForPlaywrightLoginResultAsync();
            }
            else
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaGetQrCodeFailed)], qrResult.Message);
                IsLoading = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaQrModeException)], ex.Message);
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
        StatusMessage = Localizer[nameof(UIText.PtaSwitchedToPasswordMode)];
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

            StatusMessage = Localizer[nameof(UIText.PtaLoginSuccess)];
            await _playwrightService.CleanupAsync();
        }
        else
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoginTimeoutOrFailed)], result.Message);
        }
    }

    [RelayCommand]
    private void OpenBrowserLogin()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage = Localizer[nameof(UIText.PtaPleaseEnterEmailAndPassword)];
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
            StatusMessage = Localizer[nameof(UIText.PtaBrowserLoginInstructions)];
            DebugInfo = Localizer[nameof(UIText.PtaHowToGetSession)];
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaOpenBrowserFailed)], ex.Message);
            DebugInfo += $"\n✗ 错误: {ex.Message}";
        }
    }

    [RelayCommand]
    private void SubmitCookie()
    {
        if (string.IsNullOrWhiteSpace(PtaSessionInput))
        {
            StatusMessage = Localizer[nameof(UIText.PtaPleaseEnterSessionValue)];
            return;
        }

        try
        {
            DebugInfo = $"{Localizer[nameof(UIText.PtaLoggingInWithCookie)]}\nPTASession 长度: {PtaSessionInput.Trim().Length}";

            var result = _ptaAuthService.LoginWithSession(Email, Password, PtaSessionInput.Trim());

            if (result.IsSuccess)
            {
                StatusMessage = Localizer[nameof(UIText.PtaLoginSuccess)];
                DebugInfo += $"\n✓ {Localizer[nameof(UIText.PtaCookieLoginSuccess)]}";
                ShowCookieInput = false;
                NeedsCaptcha = false;
                Password = string.Empty;
                PtaSessionInput = string.Empty;
            }
            else
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoginFailedWithReason)], result.Message);
                DebugInfo += $"\n✗ 登录失败: {result.Message}";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoginException)], ex.Message);
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
        StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoggedInAs)], _ptaAuthService.Email);
    }

    private void HandleLogout()
    {
        IsLoggedIn = false;
        StatusMessage = Localizer[nameof(UIText.PtaLoggedOut)];
        Email = string.Empty;
        Password = string.Empty;
    }

    ~PtaLoginViewModel()
    {
        _ptaAuthService.OnLogin -= HandleLogin;
        _ptaAuthService.OnLogout -= HandleLogout;
    }
}