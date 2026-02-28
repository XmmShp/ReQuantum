using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Services;
using ReQuantum.Views;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton)]
public partial class PtaLoginViewModel : ViewModelBase<PtaLoginView>, IDisposable
{
    private readonly IPtaBrowserAuthService _authService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoggedIn;

    public LocalizedText StatusMessage { get; } = new();

    public PtaLoginViewModel(IPtaBrowserAuthService authService)
    {
        _authService = authService;
        _isLoggedIn = _authService.IsAuthenticated;

        // 订阅登录/登出事件
        _authService.OnLogin += HandleLogin;
        _authService.OnLogout += HandleLogout;

        if (_isLoggedIn)
        {
            StatusMessage.Set(nameof(UIText.PtaLoggedInAs), _authService.Email);
        }
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsLoading) return;

        IsLoading = true;
        StatusMessage.Set(nameof(UIText.PtaLoggingIn));

        try
        {
            var result = await _authService.OpenBrowserAndWaitForLoginAsync(
                progressMessage => StatusMessage.Set(string.Empty), // 进度消息暂时清空，因为是原始字符串
                timeoutSeconds: 300
            );

            IsLoading = false;

            if (!result.IsSuccess)
            {
                StatusMessage.Set(nameof(UIText.BrowserLoginFailed), result.Message);
            }
            // 登录成功时，HandleLogin() 已经通过事件设置了正确的消息（"你好：XXX"），所以这里不需要再设置
        }
        catch (Exception ex)
        {
            IsLoading = false;
            StatusMessage.Set(nameof(UIText.BrowserLoginException), ex.Message);
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
        StatusMessage.Set(nameof(UIText.PtaLoggedInAs), _authService.Email);
    }

    private void HandleLogout()
    {
        IsLoggedIn = false;
        StatusMessage.Set(nameof(UIText.PtaLoggedOut));
    }

    public void Dispose()
    {
        _authService.OnLogin -= HandleLogin;
        _authService.OnLogout -= HandleLogout;
        StatusMessage.Dispose();
    }
}
