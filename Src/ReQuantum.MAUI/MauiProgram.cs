using LoginZju;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NOF.Application;
using NOF.Hosting.Maui;
using NOF.Infrastructure.EntityFrameworkCore;
using NOF.Infrastructure.EntityFrameworkCore.SQLite;
using ReQuantum.Application.Calendar;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.CoursesZju.Services;
using ReQuantum.Application.Pta.Services;
using ReQuantum.Application.ZjuSso.Services;
using ReQuantum.Contract;
using ReQuantum.Contract.Common.Services;
using ReQuantum.Infrastructure.Services;
using ReQuantum.MAUI.Persistence;
using ReQuantum.Services;
using ReQuantum.UI.Services;

namespace ReQuantum;

public static class MauiProgram
{
    public static NOFMauiApp CreateNOFMauiApp()
    {
        var builder = NOFMauiAppBuilder.Create();
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "requantum.db");

        builder.MauiAppBuilder.UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.Configure<MapperOptions>(o => o.ConfigureAutoMappings());
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddReQuantumAutoInjectServices();
        builder.Services.AddAllHandlers();
        builder.Services.AddScoped<IBackgroundTask, CoursesZjuService>();
        builder.Services.AddHostedService<BackgroundTaskHostedService>();
        builder.Services.AddSingleton<IReQuantumService, RequestSenderReQuantumService>();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:sqlite"] = $"Data Source={dbPath}"
        });

        builder.AddEFCore<ReQuantumMauiDbContext>()
            .AutoMigrate()
            .UseSqlite();

        builder.Services.AddSingleton<IEncryptor, MauiEncryptor>();
        builder.Services.AddSingleton(_ => new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(100)
        });
        builder.Services.AddLoginZju();
        builder.Services.AddSingleton<IZjuAuthAccessor, ZjuAuthAccessor>(sp => sp.GetRequiredService<ZjuAuthAccessor>());
        builder.Services.AddSingleton<IZjuContext, ZjuContext>();
        builder.Services.AddSingleton<IPtaBrowserAuthService, MauiPtaBrowserAuthService>();
        builder.Services.AddLocalization();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.BuildAsync().GetAwaiter().GetResult();
    }
    public static MauiApp CreateMauiApp()
    {
        var app = CreateNOFMauiApp();

        _ = app.Services.GetServices<IWarmup>()
            .Select(o => o.WarmupAsync())
            .ToArray();

        app.StartAsync();
        return app.MauiApp;
    }
}
