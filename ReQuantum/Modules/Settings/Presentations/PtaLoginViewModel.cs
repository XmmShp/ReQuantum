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
    private readonly IPtaBrowserAuthService _authService;

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

    public PtaLoginViewModel(IPtaBrowserAuthService authService)
    {
        _authService = authService;
        _isLoggedIn = _authService.IsAuthenticated;

        // 默认使用二维码登录模式
        _isQrLoginMode = true;

        // 订阅登录/登出事件
        _authService.OnLogin += HandleLogin;
        _authService.OnLogout += HandleLogout;

        if (_isLoggedIn)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoggedInAs)], _authService.Email);
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
            var initResult = await _authService.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaInitFailed)], initResult.Message);
                IsLoading = false;
                return;
            }

            var qrResult = await _authService.GetQrCodeAsync();

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

        // 账号密码登录直接使用浏览器自动化
        _ = StartBrowserLoginAsync();
    }

    [RelayCommand]
    private async Task SubmitCaptchaAsync()
    {
        if (string.IsNullOrWhiteSpace(CaptchaCode)) return;

        IsLoading = true;
        StatusMessage = Localizer[nameof(UIText.PtaSubmittingCaptcha)];

        var result = await _authService.SubmitCaptchaAsync(CaptchaCode);
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
            // 先彻底清理，确保从干净的状态开始
            await _authService.CleanupAsync();

            var initResult = await _authService.InitializeAsync();
            if (!initResult.IsSuccess)
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaInitFailed)], initResult.Message);
                IsLoading = false;
                return;
            }

            var qrResult = await _authService.GetQrCodeAsync();

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
        await _authService.CleanupAsync();
        StatusMessage = Localizer[nameof(UIText.PtaSwitchedToPasswordMode)];
    }

    private async Task StartBrowserLoginAsync()
    {
        try
        {
            var result = await _authService.OpenBrowserAndWaitForLoginAsync(
                Email,
                Password,
                progressMessage => StatusMessage = progressMessage,
                timeoutSeconds: 300
            );

            IsLoading = false;

            if (result.IsSuccess)
            {
                var session = result.Value;
                _authService.LoginWithSession(Email, Password, session);
                StatusMessage = Localizer[nameof(UIText.PtaLoginSuccess)];
                Password = string.Empty;
            }
            else
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.BrowserLoginFailed)], result.Message);
            }
        }
        catch (Exception ex)
        {
            IsLoading = false;
            StatusMessage = string.Format(Localizer[nameof(UIText.BrowserLoginException)], ex.Message);
        }
    }

    private async Task WaitForPlaywrightLoginResultAsync(int timeoutSeconds = 200)
    {
        while (IsQrLoginMode) // 只要还在二维码登录模式，就持续尝试
        {
            var result = await _authService.WaitForLoginSuccessAsync(timeoutSeconds);

            if (result.IsSuccess)
            {
                var session = result.Value;
                // 登录成功
                // 如果是二维码登录，Email/Password 可能是空的，使用占位符
                var email = !string.IsNullOrEmpty(Email) ? Email : "WeChatUser";
                var password = !string.IsNullOrEmpty(Password) ? Password : "QrLogin";

                _authService.LoginWithSession(email, password, session);

                StatusMessage = Localizer[nameof(UIText.PtaLoginSuccess)];
                IsLoading = false;
                await _authService.CleanupAsync();
                return; // 登录成功，退出循环
            }
            else
            {
                // 超时或失败，自动刷新二维码
                if (!IsQrLoginMode)
                {
                    // 用户已切换到其他模式，停止刷新
                    return;
                }

                StatusMessage = Localizer[nameof(UIText.QrCodeExpiredRefreshing)];

                try
                {
                    IsLoading = true;

                    // 清理旧的浏览器实例
                    await _authService.CleanupAsync();

                    // 重新初始化
                    var initResult = await _authService.InitializeAsync();
                    if (!initResult.IsSuccess)
                    {
                        StatusMessage = string.Format(Localizer[nameof(UIText.PtaInitFailed)], initResult.Message);
                        IsLoading = false;
                        return;
                    }

                    // 获取新的二维码
                    var qrResult = await _authService.GetQrCodeAsync();
                    if (qrResult.IsSuccess)
                    {
                        QrCodeBitmap = new Bitmap(qrResult.Value);
                        StatusMessage = Localizer[nameof(UIText.PtaScanQrCode)];
                        IsLoading = false;
                        // 继续循环，等待下一次扫码
                    }
                    else
                    {
                        StatusMessage = string.Format(Localizer[nameof(UIText.PtaGetQrCodeFailed)], qrResult.Message);
                        IsLoading = false;
                        return; // 获取二维码失败，退出
                    }
                }
                catch (Exception ex)
                {
                    StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoadQrCodeException)], ex.Message);
                    IsLoading = false;
                    return; // 异常，退出
                }
            }
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
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaOpenBrowserFailed)], ex.Message);
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
            var result = _authService.LoginWithSession(Email, Password, PtaSessionInput.Trim());

            if (result.IsSuccess)
            {
                StatusMessage = Localizer[nameof(UIText.PtaLoginSuccess)];
                ShowCookieInput = false;
                NeedsCaptcha = false;
                Password = string.Empty;
                PtaSessionInput = string.Empty;
            }
            else
            {
                StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoginFailedWithReason)], result.Message);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoginException)], ex.Message);
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
        _authService.Logout();
        IsQrLoginMode = false;
        IsCaptchaVisible = false;
        QrCodeBitmap = null;
        CaptchaBitmap = null;
        _authService.CleanupAsync();
    }

    private void HandleLogin()
    {
        IsLoggedIn = true;
        StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoggedInAs)], _authService.Email);
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
        _authService.OnLogin -= HandleLogin;
        _authService.OnLogout -= HandleLogout;
    }
}