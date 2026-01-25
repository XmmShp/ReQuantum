using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Entities;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Views;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(GeneralSettingsViewModel)])]
public partial class GeneralSettingsViewModel : ViewModelBase<GeneralSettingsView>, IDisposable
{
    public partial class ThemeOption : ObservableObject
    {
        public LocalizedText Name { get; }
        public ThemeVariant Variant { get; }

        public ThemeOption(string nameKey, ThemeVariant variant)
        {
            Name = new LocalizedText();
            Name.Set(nameKey);
            Variant = variant;
        }
    }

    public ObservableCollection<ThemeOption> ThemeOptions { get; }

    [ObservableProperty]
    private ThemeOption _selectedThemeOption;

    partial void OnSelectedThemeOptionChanged(ThemeOption value)
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = value.Variant;
        }
    }

    public GeneralSettingsViewModel()
    {
        ThemeOptions = new ObservableCollection<ThemeOption>
        {
            new(nameof(UIText.FollowingSystem), ThemeVariant.Default),
            new(nameof(UIText.Light), ThemeVariant.Light),
            new(nameof(UIText.Dark), ThemeVariant.Dark)
        };

        var current = Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        _selectedThemeOption = ThemeOptions.FirstOrDefault(x => x.Variant == current) ?? ThemeOptions[0];
    }

    public void Dispose()
    {
        foreach (var option in ThemeOptions)
        {
            option.Name.Dispose();
        }
    }
}
