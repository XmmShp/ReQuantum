using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Zdbk.Models;

namespace ReQuantum.Modules.Zdbk.Services;

public interface IAcademicCalendarService
{
    /// <summary>
    /// 获取当前校历
    /// </summary>
    Task<Result<AcademicCalendar>> GetCurrentCalendarAsync();

    /// <summary>
    /// 刷新校历
    /// </summary>
    Task<Result<AcademicCalendar>> RefreshCalendarAsync();

    /// <summary>
    /// 获取缓存的校历
    /// </summary>
    AcademicCalendar? GetCachedCalendar();
}

[AutoInject(Lifetime.Singleton)]
public class AcademicCalendarService : IAcademicCalendarService, IDaemonService
{
    private readonly IStorage _storage;
    private readonly ILogger<AcademicCalendarService> _logger;
    private readonly HttpClient _httpClient;

    private const string StorageKey = "Zdbk:AcademicCalendar";
    private const string CalendarApiUrl = "https://api.example.com/zju/calendar/current"; // TODO: 替换为实际的 API 地址
    private const string FallbackCalendarFilePath = "Data/calendar.json"; // Fallback校历数据文件路径

    private AcademicCalendar? _cachedCalendar;

    public AcademicCalendarService(
        IStorage storage,
        ILogger<AcademicCalendarService> logger,
        HttpClient httpClient)
    {
        _storage = storage;
        _logger = logger;
        _httpClient = httpClient;

        // 启动时加载缓存
        LoadCachedCalendar();
    }

    public async Task<Result<AcademicCalendar>> GetCurrentCalendarAsync()
    {
        // 如果有缓存且版本有效，直接返回
        if (_cachedCalendar != null)
        {
            _logger.LogDebug("Using cached calendar version {Version}", _cachedCalendar.Version);
            return Result.Success(_cachedCalendar);
        }

        // 否则从服务器获取
        return await RefreshCalendarAsync();
    }

    public async Task<Result<AcademicCalendar>> RefreshCalendarAsync()
    {
        _logger.LogInformation("Fetching academic calendar from server");

        try
        {
            var response = await _httpClient.GetAsync(CalendarApiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to fetch calendar: {StatusCode}", response.StatusCode);
                // 服务器获取失败，尝试从本地文件读取
                return await LoadFromFallbackFileAsync();
            }

            var calendarResponse = await response.Content.ReadFromJsonAsync(
                SourceGenerationContext.Default.AcademicCalendarResponse);

            if (calendarResponse is not { Success: true } || calendarResponse.Data == null)
            {
                _logger.LogError("Invalid calendar response: {Message}", calendarResponse?.Message);
                // 服务器返回无效，尝试从本地文件读取
                return await LoadFromFallbackFileAsync();
            }

            // 更新缓存
            _cachedCalendar = calendarResponse.Data;
            SaveCalendar(_cachedCalendar);

            _logger.LogInformation(
                "Successfully fetched calendar: {Semester} ({StartDate} - {EndDate}), version {Version}",
                _cachedCalendar.SemesterName,
                _cachedCalendar.StartDate,
                _cachedCalendar.EndDate,
                _cachedCalendar.Version);

            return Result.Success(_cachedCalendar);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error when fetching calendar");
            // 网络错误，尝试从本地文件读取
            return await LoadFromFallbackFileAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when fetching calendar");
            return Result.Fail($"获取校历失败：{ex.Message}");
        }
    }

    public AcademicCalendar? GetCachedCalendar()
    {
        return _cachedCalendar;
    }

    /// <summary>
    /// 从Fallback文件加载校历
    /// </summary>
    private async Task<Result<AcademicCalendar>> LoadFromFallbackFileAsync()
    {
        try
        {
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var fallbackFilePath = Path.Combine(appDirectory, FallbackCalendarFilePath);

            if (!File.Exists(fallbackFilePath))
            {
                _logger.LogError("Fallback calendar file not found: {Path}", fallbackFilePath);
                return Result.Fail($"Fallback文件不存在: {fallbackFilePath}");
            }

            _logger.LogInformation("Loading calendar from fallback file: {Path}", fallbackFilePath);

            var jsonContent = await File.ReadAllTextAsync(fallbackFilePath);
            var calendar = JsonSerializer.Deserialize(
                jsonContent,
                SourceGenerationContext.Default.AcademicCalendar);

            if (calendar == null)
            {
                _logger.LogError("Failed to parse fallback calendar file");
                return Result.Fail("Fallback文件解析失败");
            }

            // 更新缓存
            _cachedCalendar = calendar;
            SaveCalendar(_cachedCalendar);

            _logger.LogInformation(
                "Successfully loaded calendar from fallback file: {Semester} ({StartDate} - {EndDate}), version {Version}",
                _cachedCalendar.SemesterName,
                _cachedCalendar.StartDate,
                _cachedCalendar.EndDate,
                _cachedCalendar.Version);

            return Result.Success(_cachedCalendar);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load calendar from fallback file");
            return Result.Fail($"读取Fallback文件失败：{ex.Message}");
        }
    }

    private void LoadCachedCalendar()
    {
        if (_storage.TryGet<AcademicCalendar>(StorageKey, out var calendar) && calendar != null)
        {
            _cachedCalendar = calendar;
            _logger.LogInformation(
                "Loaded cached calendar: {Semester}, version {Version}",
                calendar.SemesterName,
                calendar.Version);
        }
        else
        {
            _logger.LogInformation("No cached calendar found");
        }
    }

    private void SaveCalendar(AcademicCalendar calendar)
    {
        _storage.Set(StorageKey, calendar);
        _logger.LogDebug("Saved calendar to storage");
    }
}
