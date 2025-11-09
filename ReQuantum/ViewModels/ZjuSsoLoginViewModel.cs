using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Attributes;
using ReQuantum.Infrastructure;
using ReQuantum.Resources.I18n;
using ReQuantum.Services;
using ReQuantum.Views;
using System;
using System.Threading.Tasks;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Transient, RegisterTypes = [typeof(ZjuSsoLoginViewModel)])]
public partial class ZjuSsoLoginViewModel : ViewModelBase<ZjuSsoLoginView>
{
    private readonly IZjuSsoService _zjuSsoService;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LocalizedText StatusMessage { get; }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoggedIn;

    public ZjuSsoLoginViewModel(IZjuSsoService zjuSsoService)
    {
        _zjuSsoService = zjuSsoService;
        _isLoggedIn = _zjuSsoService.IsAuthenticated;
        _zjuSsoService.OnLogout += OnLogout;
        StatusMessage = new LocalizedText(Localizer);

        if (!_zjuSsoService.IsAuthenticated)
        {
            return;
        }

        Username = _zjuSsoService.Id;
        StatusMessage.Set(nameof(UIText.AlreadyLoggedIn));
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            StatusMessage.Set(nameof(UIText.PleaseEnterUsername));
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            StatusMessage.Set(nameof(UIText.PleaseEnterPassword));
            return;
        }

        IsLoading = true;
        StatusMessage.Set(nameof(UIText.LoggingIn));

        try
        {
            var result = await _zjuSsoService.LoginAsync(Username, Password);

            if (result.IsSuccess)
            {
                StatusMessage.Set(result.Message);
                IsLoggedIn = true;
                Password = string.Empty;
            }
            else
            {
                StatusMessage.Set(result.Message);
                IsLoggedIn = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage.Set(nameof(UIText.LoginFailed), ex.Message);
            IsLoggedIn = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Logout()
    {
        _zjuSsoService.Logout();
        IsLoggedIn = false;
        Username = string.Empty;
        Password = string.Empty;
        StatusMessage.Set(nameof(UIText.LogoutSuccessful));
    }

    private void OnLogout()
    {
        IsLoggedIn = false;
        StatusMessage.Set(nameof(UIText.LogoutSuccessful));
    }

    public override void Dispose()
    {
        StatusMessage.Dispose();
        _zjuSsoService.OnLogout -= OnLogout;
        base.Dispose();
    }
}
