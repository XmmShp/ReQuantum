using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IconPacks.Avalonia.Material;
using Microsoft.Extensions.DependencyInjection;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Menu.Abstractions;
using LocalizedText = ReQuantum.Infrastructure.Entities.LocalizedText;

namespace ReQuantum.Services;

public interface IMenuManager
{
    ObservableCollection<MenuItem> MenuItems { get; }
    MenuItem? SelectedItem { get; }
}

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(IMenuManager), typeof(IDaemonService)])]
public partial class MenuManager : ObservableObject, IMenuManager, IDaemonService
{
    [ObservableProperty]
    private ObservableCollection<MenuItem> _menuItems = [];

    [ObservableProperty]
    private MenuItem? _selectedItem;

    public MenuManager(IServiceProvider serviceProvider)
    {
        Initialize(serviceProvider);
    }

    private void Initialize(IServiceProvider serviceProvider)
    {
        var providers = serviceProvider.GetServices<IMenuItemProvider>();
        var menuItemPairs = providers.OrderBy(p => p.Order)
            .Select(p => p.MenuItem)
            .ToList();
        foreach (var pair in menuItemPairs)
        {
            MenuItems.Add(pair);
        }
    }
}

public partial class MenuItem : ObservableObject
{
    public required LocalizedText Title { get; init; }

    [ObservableProperty]
    private PackIconMaterialKind _iconKind;

    public Action? OnSelected { get; init; }
    public Action? OnUnselected { get; init; }

    [RelayCommand]
    private void Selected()
    {
        OnSelected?.Invoke();
    }

    [RelayCommand]
    private void Unselected()
    {
        OnUnselected?.Invoke();
    }
}
