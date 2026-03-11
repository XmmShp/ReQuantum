using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NOF.Hosting.Maui;
using NOF.Infrastructure.EntityFrameworkCore;
using NOF.Infrastructure.EntityFrameworkCore.SQLite;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Domain.Calendar;
using ReQuantum.Infrastructure.Persistence.Repositories;
using ReQuantum.Infrastructure.Services;
using ReQuantum.MAUI.Persistence;
using ReQuantum.Shared.Services;
using System.Collections.Generic;
using System.IO;

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

        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddReQuantumAutoInjectServices();
        builder.Services.AddAllHandlers();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:sqlite"] = $"Data Source={dbPath}"
        });

        builder.AddEFCore<ReQuantumMauiDbContext>()
            .AutoMigrate()
            .UseSqlite();

        builder.Services.AddSingleton<IStorage, SqliteStorage>();
        builder.Services.AddSingleton<ICalendarEventRepository, SqliteCalendarEventRepository>();
        builder.Services.AddSingleton<ICalendarTodoRepository, SqliteCalendarTodoRepository>();
        builder.Services.AddSingleton<ICalendarNoteRepository, SqliteCalendarNoteRepository>();
        builder.Services.AddSingleton<IBrowserLoginProvider, PlaywrightBrowserLoginProvider>();
        builder.Services.AddSingleton<HttpClient>();
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
