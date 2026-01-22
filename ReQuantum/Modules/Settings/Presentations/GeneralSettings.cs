using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Views;
using System.Linq;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(GeneralSettingsViewModel)])]
public partial class GeneralSettingsViewModel : ViewModelBase<GeneralSettingsView>
{
    public record ThemeOption(string Name, ThemeVariant Variant);

    public ThemeOption[] ThemeOptions { get; } =
    [
        new(UIText.FollowingSystem, ThemeVariant.Default),
        new(UIText.Light, ThemeVariant.Light),
        new(UIText.Dark, ThemeVariant.Dark)
    ];

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
        var current = Application.Current?.RequestedThemeVariant ?? ThemeVariant.Default;
        _selectedThemeOption = ThemeOptions.FirstOrDefault(x => x.Variant == current) ?? ThemeOptions[0];
    }
}
