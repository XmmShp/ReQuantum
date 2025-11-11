using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Material;
using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Models;
using ReQuantum.Resources.I18n;
using ReQuantum.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(SettingsViewModel), typeof(IMenuItemProvider)])]
public partial class SettingsViewModel : ViewModelBase<SettingsView>, IMenuItemProvider
{
    #region MenuItemProvider APIs
    public string Title => UIText.Settings;
    public PackIconMaterialKind IconKind => PackIconMaterialKind.Cog;
    public uint Order => uint.MaxValue;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ViewModelType => typeof(SettingsViewModel);
    Action<MenuItem> IMenuItemProvider.OnCultureChanged => item => item.Title = UIText.Settings;
    #endregion

    public ZjuSsoLoginViewModel ZjuSsoLoginViewModel { get; }

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public List<LanguageOption> AvailableLanguages { get; } =
    [
        new("English", "en-US"),
        new("中文", "zh-CN")
    ];

    public SettingsViewModel(ZjuSsoLoginViewModel zjuSsoLoginViewModel)
    {
        ZjuSsoLoginViewModel = zjuSsoLoginViewModel;

        // Set current language
        var currentCulture = System.Globalization.CultureInfo.CurrentUICulture.Name;
        _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentCulture)
                            ?? AvailableLanguages[0];
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        Localizer.SetCulture(value.CultureCode);
    }

    public override void Dispose()
    {
        ZjuSsoLoginViewModel.Dispose();
        base.Dispose();
    }
}

public class LanguageOption
{
    public string DisplayName { get; }
    public string CultureCode { get; }

    public LanguageOption(string displayName, string cultureCode)
    {
        DisplayName = displayName;
        CultureCode = cultureCode;
    }

    public override bool Equals(object? obj)
    {
        return obj is LanguageOption option && CultureCode == option.CultureCode;
    }

    public override int GetHashCode()
    {
        return CultureCode.GetHashCode();
    }
}
