using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.Zdbk.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReQuantum.Application.Zdbk.Services;

[AutoInject(Lifetime.Singleton)]
public class AcademicCalendarService : IAcademicCalendarService
{
    private readonly IStorage _storage;
    private readonly ILogger<AcademicCalendarService> _logger;
    private readonly HttpClient _httpClient;

    private const string StorageKey = "Zdbk:AcademicCalendar";
    private const string CalendarApiUrl = "https://api.example.com/zju/calendar/current";
    private const string FallbackCalendarFilePath = "Data/calendar.json";

    private AcademicCalendar? _cachedCalendar;

    public AcademicCalendarService(IStorage storage, ILogger<AcademicCalendarService> logger, HttpClient httpClient)
    {
        _storage = storage;
        _logger = logger;
        _httpClient = httpClient;
        LoadCachedCalendar();
    }

    public async Task<Result<AcademicCalendar>> GetCurrentCalendarAsync()
    {
        if (_cachedCalendar != null)
        {
            return _cachedCalendar;
        }

        return await RefreshCalendarAsync();
    }

    public async Task<Result<AcademicCalendar>> RefreshCalendarAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync(CalendarApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return await LoadFromFallbackFileAsync();
            }

            var calendarResponse = await response.Content.ReadFromJsonAsync<AcademicCalendarResponse>();
            if (calendarResponse is not { Success: true } || calendarResponse.Data == null)
            {
                return await LoadFromFallbackFileAsync();
            }

            _cachedCalendar = calendarResponse.Data;
            SaveCalendar(_cachedCalendar);
            return _cachedCalendar;
        }
        catch (HttpRequestException)
        {
            return await LoadFromFallbackFileAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when fetching calendar");
            return Result.Fail("500", $"获取校历失败：{ex.Message}");
        }
    }

    public AcademicCalendar? GetCachedCalendar() => _cachedCalendar;

    private async Task<Result<AcademicCalendar>> LoadFromFallbackFileAsync()
    {
        try
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var fallbackFilePath = Path.Combine(appDirectory, FallbackCalendarFilePath);

            if (!File.Exists(fallbackFilePath))
            {
                return Result.Fail("500", $"Fallback文件不存在: {fallbackFilePath}");
            }

            var jsonContent = await File.ReadAllTextAsync(fallbackFilePath);
            var calendar = JsonSerializer.Deserialize<AcademicCalendar>(jsonContent);

            if (calendar == null)
            {
                return Result.Fail("500", "Fallback文件解析失败");
            }

            _cachedCalendar = calendar;
            SaveCalendar(_cachedCalendar);
            return _cachedCalendar;
        }
        catch (Exception ex)
        {
            return Result.Fail("500", $"读取Fallback文件失败：{ex.Message}");
        }
    }

    private void LoadCachedCalendar()
    {
        if (_storage.TryGetAsync<AcademicCalendar>(StorageKey).GetAwaiter().GetResult() is { HasValue: true, Value: { } calendar })
        {
            _cachedCalendar = calendar;
        }
    }

    private void SaveCalendar(AcademicCalendar calendar) => _storage.SetAsync(StorageKey, calendar);
}
