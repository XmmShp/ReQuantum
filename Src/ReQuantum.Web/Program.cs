using ReQuantum.Application.Common.Services;
using ReQuantum.Application.CoursesZju.Services;
using ReQuantum.Application.Pta.Services;
using ReQuantum.Application.Zdbk.Services;
using ReQuantum.Application.ZjuSso.Abstractions;
using ReQuantum.Application.ZjuSso.Services;
using ReQuantum.Web.Components;
using ReQuantum.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<IStorage, WebStorage>();
builder.Services.AddSingleton<WebHttpContext>();
builder.Services.AddSingleton<IHttpContext>(sp => sp.GetRequiredService<WebHttpContext>());
builder.Services.AddSingleton<IZjuLoginAfterReadyHandler, CoursesZjuSessionAfterReadyHandler>();
builder.Services.AddSingleton<WebZjuContext>();
builder.Services.AddSingleton<IZjuContext>(sp => sp.GetRequiredService<WebZjuContext>());
builder.Services.AddSingleton<IMutableZjuContext>(sp => sp.GetRequiredService<WebZjuContext>());
builder.Services.AddSingleton<ICoursesZjuService, CoursesZjuService>();
builder.Services.AddSingleton<IPtaBrowserAuthService, WebPtaBrowserAuthService>();
builder.Services.AddSingleton<IPtaProblemSetService, PtaProblemSetService>();
builder.Services.AddSingleton<IPtaCalendarConvertService, PtaCalendarConvertService>();
builder.Services.AddSingleton<IAcademicCalendarService, AcademicCalendarService>();
builder.Services.AddSingleton<IZdbkCalendarConverter, ZdbkCalendarConvertService>();
builder.Services.AddSingleton<IZdbkExamService, ZdbkExamService>();
builder.Services.AddSingleton<IZdbkGradeService, ZdbkGradeService>();
builder.Services.AddSingleton<IZdbkSectionScheduleService, ZdbkSectionScheduleService>();
builder.Services.AddSingleton<HttpClient>();
builder.Services.AddLocalization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(ReQuantum.UI._Imports).Assembly,
        typeof(ReQuantum.Web.Wasm._Imports).Assembly);

app.Run();
