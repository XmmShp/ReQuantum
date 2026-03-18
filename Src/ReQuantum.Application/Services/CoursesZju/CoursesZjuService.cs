using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Models.CoursesZju;
using ReQuantum.Contract.Calendar;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace ReQuantum.Application.Services.CoursesZju;

[AutoInject(Lifetime.Singleton)]
public class CoursesZjuService : ICoursesZjuService, ICalendarTodoProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoursesZjuService> _logger;
    private const string TodoApi = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

    public string Name => "学在浙大";

    public CoursesZjuService(HttpClient httpClient, ILogger<CoursesZjuService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<HashSet<CoursesZjuTodoDto>>> GetTodoListAsync()
    {
        try
        {
            return await FetchTodoDtosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when getting todo list from courses.zju.edu.cn");
            return Result.Fail("400", $"获取待办事项失败：{ex.Message}");
        }
    }

    public async IAsyncEnumerable<CalendarTodo> GetTodosAsync(
        DateOnly start,
        DateOnly end,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        HashSet<CoursesZjuTodoDto> dtos;
        try
        {
            dtos = await FetchTodoDtosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when fetching todos from courses.zju.edu.cn for calendar");
            yield break;
        }

        foreach (var dto in dtos)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dueDate = DateOnly.FromDateTime(dto.EndTime.ToLocalTime());
            if (dueDate < start || dueDate > end)
            {
                continue;
            }

            yield return MapToCalendarTodo(dto);
        }
    }

    private async Task<HashSet<CoursesZjuTodoDto>> FetchTodoDtosAsync()
    {
        using var response = await _httpClient.GetAsync(TodoApi);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"获取待办事项失败: {response.StatusCode}");
        }

        var data = await response.Content.ReadFromJsonAsync<CoursesZjuTodosResponse>();
        if (data is null)
        {
            throw new JsonException("解析待办事项失败");
        }

        return data.TodoList.ToHashSet();
    }

    private static CalendarTodo MapToCalendarTodo(CoursesZjuTodoDto dto)
    {
        var properties = new Dictionary<string, object?>
        {
            ["course_id"] = dto.CourseId,
            ["course_name"] = dto.CourseName,
            ["course_code"] = dto.CourseCode,
            ["type"] = dto.Type,
            ["is_locked"] = dto.IsLocked,
            ["is_student"] = dto.IsStudent,
            ["submit_rate"] = dto.SubmitRate,
            ["not_scored_num"] = dto.NotScoredNum,
            ["start_time"] = dto.StartTime,
            ["source"] = "courses_zju"
        };

        return new CalendarTodo(
            Id: dto.Id,
            Content: dto.Title,
            DueTime: dto.EndTime,
            IsCompleted: false,
            CreatedAt: dto.StartTime ?? dto.EndTime,
            Properties: properties);
    }
}
