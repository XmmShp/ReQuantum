using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReQuantum.Application.Repositories.Calendar;
using ReQuantum.Application.Services.Calendar;
using ReQuantum.Application.Services.CoursesZju;
using ReQuantum.Domain.Calendar;
using ReQuantum.Application.Services.Pta;
using ReQuantum.Application.Services.Zdbk;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;
using ReQuantum.Web.Wasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<IStorage, WebClientStorage>();
builder.Services.AddSingleton<ICalendarEventRepository, StorageCalendarEventRepository>();
builder.Services.AddSingleton<ICalendarTodoRepository, StorageCalendarTodoRepository>();
builder.Services.AddSingleton<ICalendarNoteRepository, StorageCalendarNoteRepository>();
builder.Services.AddSingleton<ICalendarService, CalendarService>();
builder.Services.AddSingleton<IBrowserLoginProvider, StubBrowserLoginProvider>();
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

await builder.Build().RunAsync();
