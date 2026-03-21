using LoginZju;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.CoursesZju.Services;
using ReQuantum.Application.Pta.Services;
using ReQuantum.Application.Zdbk.Services;
using ReQuantum.Application.ZjuSso.Services;
using ReQuantum.Contract.Common.Services;
using ReQuantum.UI.Services;
using ReQuantum.Web.Wasm.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddSingleton<IStorage, WebClientStorage>();
builder.Services.AddSingleton<IEncryptor, AesEncryptor>();
builder.Services.AddLoginZju();
builder.Services.AddSingleton<IZjuAuthAccessor, ZjuAuthAccessor>();
builder.Services.AddSingleton<IZjuContext, ZjuContext>();
builder.Services.AddSingleton<ICoursesZjuService, CoursesZjuService>();
builder.Services.AddSingleton<IPtaBrowserAuthService, WasmPtaBrowserAuthService>();
builder.Services.AddSingleton<IPtaProblemSetService, PtaProblemSetService>();
builder.Services.AddSingleton<IPtaCalendarConvertService, PtaCalendarConvertService>();
builder.Services.AddSingleton<IAcademicCalendarService, AcademicCalendarService>();
builder.Services.AddSingleton<IZdbkCalendarConverter, ZdbkCalendarConvertService>();
builder.Services.AddSingleton<IZdbkExamService, ZdbkExamService>();
builder.Services.AddSingleton<IZdbkGradeService, ZdbkGradeService>();
builder.Services.AddSingleton<IZdbkSectionScheduleService, ZdbkSectionScheduleService>();
builder.Services.AddSingleton(_ => new HttpClient());
builder.Services.AddLocalization();

await builder.Build().RunAsync();
