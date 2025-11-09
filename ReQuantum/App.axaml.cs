using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using ReQuantum.Abstractions;
using ReQuantum.Extensions;
using ReQuantum.Generated;
using ReQuantum.Options;
using ReQuantum.Services;
using ReQuantum.Shells;
using ReQuantum.ViewModels;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ReQuantum;

public partial class App : Application
{
    private IServiceProvider _serviceProvider = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture;
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging();
        serviceCollection.AddSingleton<IDataTemplate, GeneratedViewLocator>();
        serviceCollection.AutoAddGeneratedServices();

        serviceCollection.AddSingleton(typeof(IMenuItemAccessor<>), typeof(MenuItemAccessorFactory<>));

        serviceCollection.Configure<StorageOptions>(options =>
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var appFolderPath = Path.Combine(appDataPath, "ReQuantum");
            Directory.CreateDirectory(appFolderPath);
            options.StoragePath = Path.Combine(appFolderPath, "ReQuantum.db");
        });

        _serviceProvider = serviceCollection.BuildServiceProvider();

        SingletonManager.Instance.Configure(_serviceProvider);

        var initializableObjects = _serviceProvider.GetServices<IInitializable>();
        Task.WhenAll(initializableObjects.Select(i => i.InitializeAsync(_serviceProvider))).Wait();

        var navigator = _serviceProvider.GetRequiredService<INavigator>();
        var navigationMenuService = _serviceProvider.GetRequiredService<IMenuManager>();
        var storage = _serviceProvider.GetRequiredService<IStorage>();
        var windowService = _serviceProvider.GetRequiredService<IWindowService>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new ShellWindow
            {
                DataContext = new ShellViewModel(navigator, navigationMenuService, storage)
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new ShellView
            {
                DataContext = new ShellViewModel(navigator, navigationMenuService, storage)
            };
        }

        navigator.NavigateTo<DashboardViewModel>();
        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}