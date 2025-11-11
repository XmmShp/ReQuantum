using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Material;
using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Models;
using ReQuantum.Resources.I18n;
using ReQuantum.Services;
using ReQuantum.Views;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ReQuantum.ViewModels;

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(DashboardViewModel), typeof(IMenuItemProvider)])]
public partial class DashboardViewModel : ViewModelBase<DashboardView>, IMenuItemProvider
{
    #region MenuItemProvider APIs
    public string Title => UIText.Home;
    public PackIconMaterialKind IconKind => PackIconMaterialKind.ViewDashboard;
    public uint Order => 0;

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    public Type ViewModelType => typeof(DashboardViewModel);
    Action<MenuItem> IMenuItemProvider.OnCultureChanged => item => item.Title = UIText.Home;
    #endregion

    private readonly ILocalizer _localizer;

    public string Welcome => _localizer[UIText.HelloWorld];

    public DashboardViewModel(ILocalizer localizer)
    {
        _localizer = localizer;
    }

    [RelayCommand]
    private void UpdateWelcome()
    {
        _localizer.SetCulture("zh-CN");
    }
}
