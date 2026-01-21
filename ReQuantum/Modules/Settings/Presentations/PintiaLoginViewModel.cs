using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.PintiaLogin.Services;
using ReQuantum.Views;
using System;
using System.Threading.Tasks;
using LocalizedText = ReQuantum.Infrastructure.Entities.LocalizedText;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(PintiaLoginViewModel)])]
public partial class PintiaLoginViewModel : ViewModelBase<PintiaLoginView>
{
    private readonly IPintiaService _pintiaService;
    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    public LocalizedText StatusMessage { get; }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoggedIn;

    public PintiaLoginViewModel(IPintiaService pintiaService)
    {
        _pintiaService = pintiaService;
        _isLoggedIn = _pintiaService.IsAuthenticated;
        _pintiaService.OnLogout += OnLogout;
        StatusMessage = new LocalizedText();

        if (!_pintiaService.IsAuthenticated)
        {
            return;
        }

        Username = _pintiaService.Id ?? string.Empty;
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
            var result = await _pintiaService.LoginAsync(Username, Password);

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
        _pintiaService.Logout();
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

    [RelayCommand]
    private async void ShowQtCode()
    {

    }

}
