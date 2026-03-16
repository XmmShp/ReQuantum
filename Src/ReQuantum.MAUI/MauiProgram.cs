using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NOF.Application;
using NOF.Hosting.Maui;
using NOF.Infrastructure.EntityFrameworkCore;
using NOF.Infrastructure.EntityFrameworkCore.SQLite;
using ReQuantum.Application.RequestHandlers;
using ReQuantum.Application.Services;
using ReQuantum.Application.Services.CoursesZju;
using ReQuantum.Application.Services.Pta;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Contract;
using ReQuantum.Infrastructure.Services;
using ReQuantum.MAUI.Persistence;

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
        builder.Services.AddSingleton<IReQuantumService, RequestSenderReQuantumService>();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:sqlite"] = $"Data Source={dbPath}"
        });

        builder.AddEFCore<ReQuantumMauiDbContext>()
            .AutoMigrate()
            .UseSqlite();

        builder.Services.AddSingleton<MauiHttpContext>();
        builder.Services.AddSingleton<IHttpContext>(sp => sp.GetRequiredService<MauiHttpContext>());
        builder.Services.AddSingleton<HttpClient>(sp => sp.GetRequiredService<MauiHttpContext>().HttpClient);
        builder.Services.AddSingleton<IZjuLoginAfterReadyHandler, CoursesZjuSessionAfterReadyHandler>();
        builder.Services.AddSingleton<MauiZjuContext>();
        builder.Services.AddSingleton<IZjuContext>(sp => sp.GetRequiredService<MauiZjuContext>());
        builder.Services.AddSingleton<IMutableZjuContext>(sp => sp.GetRequiredService<MauiZjuContext>());
        builder.Services.AddSingleton<IZjuAuthenticator>(sp => sp.GetRequiredService<MauiZjuContext>());
        builder.Services.AddSingleton<IZjuLoginStateWriter>(sp => sp.GetRequiredService<MauiZjuContext>());
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
        return CreateNOFMauiApp().MauiApp;
    }
}
