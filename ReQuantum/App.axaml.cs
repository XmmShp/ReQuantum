using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Data.Core.Plugins;
using Avalonia.Logging;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReQuantum.Abstractions;
using ReQuantum.Extensions;
using ReQuantum.Generated;
using ReQuantum.Options;
using ReQuantum.Services;
using ReQuantum.Shells;
using ReQuantum.ViewModels;
using Serilog;
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
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var logger = new LoggerConfiguration()
#if RELEASE
            .MinimumLevel.Warning()
            .WriteTo.File(
                Path.Combine(appDataPath, "ReQuantum", "logs", "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10_000_000)
#else
            .MinimumLevel.Debug()
            .WriteTo.Debug()
#endif
            .CreateLogger();

        Log.Logger = logger;

        Logger.Sink = new SerilogAvaloniaLogSink(logger);

        AppDomain.CurrentDomain.UnhandledException += (_, e)
            => Log.Write(Serilog.Events.LogEventLevel.Error,
                (Exception)e.ExceptionObject,
                "Unhandled exception");

        TaskScheduler.UnobservedTaskException += (_, e)
            => Log.Write(Serilog.Events.LogEventLevel.Error,
                e.Exception,
                "Unobserved task exception");

        CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture;
        var serviceCollection = new ServiceCollection();

        serviceCollection.AddLogging(loggingBuilder => loggingBuilder.ClearProviders().AddSerilog(dispose: true));

        serviceCollection.AddSingleton<IDataTemplate, GeneratedViewLocator>();
        serviceCollection.AutoAddGeneratedServices();

        serviceCollection.AddSingleton(typeof(IMenuItemAccessor<>), typeof(MenuItemAccessorFactory<>));

        serviceCollection.Configure<StorageOptions>(options =>
        {
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

    private static void DisableAvaloniaDataAnnotationValidation()
    {
#pragma warning disable IL2026
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
#pragma warning restore IL2026
    }
}

public class SerilogAvaloniaLogSink : ILogSink
{
    private readonly Serilog.Core.Logger _logger;

    public SerilogAvaloniaLogSink(Serilog.Core.Logger logger)
    {
        _logger = logger;
    }

    public bool IsEnabled(LogEventLevel level, string area)
    {
        return true;
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate)
    {
        Log(level, area, source, messageTemplate, []);
    }

    public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues)
    {
        var serilogLevel = MapLevel(level);

        var sourceDescription = source switch
        {
            null => "null",
            string s => $"\"{s}\"",
            _ => source.GetType().Name
        };

        var finalMessageTemplate = messageTemplate + " [Area: {Avalonia.Area}, Source: {Avalonia.Source}]";

        _logger.ForContext("Area", area)
            .ForContext("Source", sourceDescription)
            .Write(serilogLevel, finalMessageTemplate, [.. propertyValues, serilogLevel, sourceDescription]);
    }

    private static Serilog.Events.LogEventLevel MapLevel(LogEventLevel level) => level switch
    {
        LogEventLevel.Verbose => Serilog.Events.LogEventLevel.Verbose,
        LogEventLevel.Debug => Serilog.Events.LogEventLevel.Debug,
        LogEventLevel.Information => Serilog.Events.LogEventLevel.Information,
        LogEventLevel.Warning => Serilog.Events.LogEventLevel.Warning,
        LogEventLevel.Error => Serilog.Events.LogEventLevel.Error,
        LogEventLevel.Fatal => Serilog.Events.LogEventLevel.Fatal,
        _ => Serilog.Events.LogEventLevel.Information
    };
}