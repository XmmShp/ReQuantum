using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Models.CoursesZju;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReQuantum.Application.Services.CoursesZju;

[AutoInject(Lifetime.Singleton)]
public class CoursesZjuService : ICoursesZjuService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CoursesZjuService> _logger;
    private const string TodoApi = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

    public CoursesZjuService(HttpClient httpClient, ILogger<CoursesZjuService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Result<HashSet<CoursesZjuTodoDto>>> GetTodoListAsync()
    {
        try
        {
            return await TryGetTodoListCoreAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when getting todo list from courses.zju.edu.cn");
            return Result.Fail("400", $"获取待办事项失败：{ex.Message}");
        }
    }

    private async Task<Result<HashSet<CoursesZjuTodoDto>>> TryGetTodoListCoreAsync()
    {
        using var response = await _httpClient.GetAsync(TodoApi);
        if (!response.IsSuccessStatusCode)
        {
            return Result.Fail("400", $"获取待办事项失败: {response.StatusCode}");
        }

        try
        {
            var data = await response.Content.ReadFromJsonAsync<CoursesZjuTodosResponse>();
            if (data is null)
            {
                return Result.Fail("400", "解析待办事项失败");
            }

            return data.TodoList.ToHashSet();
        }
        catch (JsonException)
        {
            return Result.Fail("400", "解析待办事项失败");
        }
    }
}
