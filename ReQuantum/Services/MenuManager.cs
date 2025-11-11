using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace ReQuantum.Services;

public interface IMenuManager
{
    IReadOnlyCollection<MenuItemPair> MenuItemPairs { get; }

    MenuItem? GetMenuItem(Type viewModelType);
    MenuItem? SelectedItem { get; set; }
}

public record MenuItemPair([property: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type ViewModelType,
    MenuItem MenuItem,
    Action<MenuItem> OnCultureChanged);

[AutoInject(Lifetime.Singleton, RegisterTypes = [typeof(IMenuManager), typeof(IInitializable)])]
public partial class MenuManager : ObservableObject, IMenuManager, IInitializable, IDisposable
{
    [ObservableProperty]
    private ObservableCollection<MenuItemPair> _menuItems = [];

    public IReadOnlyCollection<MenuItemPair> MenuItemPairs => MenuItems.AsReadOnly();

    [ObservableProperty]
    private MenuItem? _selectedItem;

    private readonly ILocalizer _localizer;

    public MenuManager(ILocalizer localizer)
    {
        _localizer = localizer;
        _localizer.CultureChanged += OnCultureChanged;
    }

    private void OnCultureChanged(CultureInfo cultureInfo)
    {
        foreach (var mip in MenuItemPairs)
        {
            mip.OnCultureChanged(mip.MenuItem);
        }
    }

    public Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var providers = serviceProvider.GetServices<IMenuItemProvider>();
        var menuItemPairs = providers.OrderBy(p => p.Order)
            .Select(p => new MenuItemPair(p.ViewModelType,
                new MenuItem
                {
                    Title = p.Title,
                    IconKind = p.IconKind
                },
                p.OnCultureChanged))
            .ToList();
        foreach (var pair in menuItemPairs)
        {
            MenuItems.Add(pair);
        }
        return Task.CompletedTask;
    }

    public MenuItem? GetMenuItem(Type viewModelType)
    {
        return MenuItems.FirstOrDefault(mi => mi.ViewModelType == viewModelType)?.MenuItem;
    }

    public void Dispose()
    {
        _localizer.CultureChanged -= OnCultureChanged;
        GC.SuppressFinalize(this);
    }
}