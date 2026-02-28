using Microsoft.Extensions.Logging;
using ReQuantum.Application.Services.Calendar;
using ReQuantum.Application.Services.CoursesZju;
using ReQuantum.Application.Services.Pta;
using ReQuantum.Application.Services.Zdbk;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Services;
using ReQuantum.Shared.Services;

namespace ReQuantum;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddSingleton<IStorage, LocalStorage>();
        builder.Services.AddSingleton<ICalendarService, CalendarService>();
        builder.Services.AddSingleton<IBrowserLoginProvider, PlaywrightBrowserLoginProvider>();
        builder.Services.AddSingleton<IZjuSsoService, ZjuSsoService>();
        builder.Services.AddSingleton<ICoursesZjuService, CoursesZjuService>();
        builder.Services.AddSingleton<IPtaBrowserAuthService, PtaBrowserAuthService>();
        builder.Services.AddSingleton<IPtaProblemSetService, PtaProblemSetService>();
        builder.Services.AddSingleton<IPtaCalendarConvertService, PtaCalendarConvertService>();
        builder.Services.AddSingleton<IAcademicCalendarService, AcademicCalendarService>();
        builder.Services.AddSingleton<IZdbkCalendarConverter, ZdbkCalendarConvertService>();
        builder.Services.AddSingleton<IZdbkExamService, ZdbkExamService>();
        builder.Services.AddSingleton<IZdbkGradeService, ZdbkGradeService>();
        builder.Services.AddSingleton<IZdbkSectionScheduleService, ZdbkSectionScheduleService>();
        builder.Services.AddSingleton<HttpClient>();
        builder.Services.AddLocalization();

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
