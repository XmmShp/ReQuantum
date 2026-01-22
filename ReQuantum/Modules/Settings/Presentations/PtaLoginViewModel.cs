using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Services;
using ReQuantum.Views;
using System;
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

    public PtaLoginViewModel(IPtaBrowserAuthService authService)
    {
        _authService = authService;
        _isLoggedIn = _authService.IsAuthenticated;

        // 订阅登录/登出事件
        _authService.OnLogin += HandleLogin;
        _authService.OnLogout += HandleLogout;

        if (_isLoggedIn)
        {
            StatusMessage = string.Format(Localizer[nameof(UIText.PtaLoggedInAs)], _authService.Email);
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage = Localizer[nameof(UIText.PtaLoggingIn)];

        try
        {
            var result = await _authService.OpenBrowserAndWaitForLoginAsync(
                progressMessage => StatusMessage = progressMessage,
                timeoutSeconds: 300
            );

            IsLoading = false;

            if (result.IsSuccess)
            {
                // 登录成功，状态已在Service中更新
                StatusMessage = Localizer[nameof(UIText.PtaLoginSuccess)];
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

    [RelayCommand]
    private void Logout()
    {
        _authService.Logout();
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
    }

    ~PtaLoginViewModel()
    {
        _authService.OnLogin -= HandleLogin;
        _authService.OnLogout -= HandleLogout;
    }
}
