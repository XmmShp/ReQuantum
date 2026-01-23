using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.ZjuSso.Services;
using ReQuantum.Views;
using LocalizedText = ReQuantum.Infrastructure.Entities.LocalizedText;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(ZjuSsoLoginViewModel)])]
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
        StatusMessage = new LocalizedText();

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
}
