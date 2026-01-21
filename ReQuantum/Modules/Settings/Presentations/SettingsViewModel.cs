using CommunityToolkit.Mvvm.ComponentModel;
using IconPacks.Avalonia.Material;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Menu.Abstractions;
using ReQuantum.Services;
using ReQuantum.ViewModels;
using ReQuantum.Views;
using System.Collections.Generic;
using System.Linq;

namespace ReQuantum.Modules.Settings.Presentations;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(SettingsViewModel), typeof(IMenuItemProvider)])]
public partial class SettingsViewModel : ViewModelBase<SettingsView>, IMenuItemProvider
{
    #region MenuItemProvider APIs
    public MenuItem MenuItem { get; }
    public uint Order => uint.MaxValue;

    #endregion

    [ObservableProperty]
    private ZjuSsoLoginViewModel _zjuSsoLoginViewModel;

    [ObservableProperty]
    private PtaLoginViewModel _ptaLoginViewModel;

    [ObservableProperty]
    private LanguageOption _selectedLanguage;

    public List<LanguageOption> AvailableLanguages { get; } =
    [
        new("English", "en-US"),
        new("中文", "zh-CN")
    ];

    public SettingsViewModel(ZjuSsoLoginViewModel zjuSsoLoginViewModel, PtaLoginViewModel ptaLoginViewModel)
    {
        MenuItem = new MenuItem()
        {
            Title = new LocalizedText { Key = nameof(UIText.Settings) },
            IconKind = PackIconMaterialKind.Cog,
            OnSelected = () => Navigator.NavigateTo<SettingsViewModel>()
        };
        ZjuSsoLoginViewModel = zjuSsoLoginViewModel;
        PtaLoginViewModel = ptaLoginViewModel;

        // Set current language
        var currentCulture = System.Globalization.CultureInfo.CurrentUICulture.Name;
        _selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentCulture)
                            ?? AvailableLanguages[0];
    }

    partial void OnSelectedLanguageChanged(LanguageOption value)
    {
        Localizer.SetCulture(value.CultureCode);
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
