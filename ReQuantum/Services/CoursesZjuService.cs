using Microsoft.Extensions.Logging;
using ReQuantum.Abstractions;
using ReQuantum.Attributes;
using ReQuantum.Client;
using ReQuantum.Extensions;
using ReQuantum.Models;
using ReQuantum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ReQuantum.Services;

public interface ICoursesZjuService
{
    Task<Result<List<CalendarTodo>>> GetTodoListAsync();
}

[AutoInject(Lifetime.Singleton)]
public class CoursesZjuService : ICoursesZjuService, IInitializable
{
    private readonly IZjuSsoService _zjuSsoService;
    private readonly IStorage _storage;
    private readonly ILogger<CoursesZjuService> _logger;
    private CoursesZjuState? _state;
    private const string StateKey = "CoursesZju:State";
    private const string TodoApi = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

    public CoursesZjuService(IZjuSsoService zjuSsoService, IStorage storage, ILogger<CoursesZjuService> logger)
    {
        _zjuSsoService = zjuSsoService;
        _storage = storage;
        _logger = logger;
        _zjuSsoService.OnLogout += () => _state = null;
    }

    public async Task<Result<List<CalendarTodo>>> GetTodoListAsync()
    {
        var clientResult = await GetAuthenticatedClient();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }

        var client = clientResult.Value;

        var result = await client.GetAsync(TodoApi);

        if (!result.IsSuccessStatusCode)
        {
            _state = null;
            return Result.Fail($"获取待办事项失败: {result.StatusCode}");
        }

        try
        {
            var response = await result.Content.ReadFromJsonAsync(SourceGenerationContext.Default.CoursesZjuTodosResponse);
            if (response is null)
            {
                return Result.Fail("解析待办事项失败");
            }

            return response.TodoList.ToHashSet()
                .Select(t => new CalendarTodo
                {
                    Id = Converter.LongToGuid(t.Id),
                    Content = $"{t.CourseName}\n{t.Title}",
                    DueTime = t.EndTime.ToLocalTime(),
                    IsCompleted = false,
                    IsFromCoursesZju = true
                }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured when getting todo list from courses.zju.edu.cn");
            return Result.Fail($"获取待办事项失败：{ex.Message}");
        }
    }

    private async Task<Result<RequestClient>> GetAuthenticatedClient()
    {
        if (_state is not null)
        {
            return Result.Success(RequestClient.Create(new RequestOptions { Cookies = [_state.Session] }));
        }

        var clientResult = await _zjuSsoService.GetAuthenticatedClientAsync(new RequestOptions { AllowRedirects = true });
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }

        var client = clientResult.Value;


        await client.GetAsync(TodoApi);
        var session = client.CookieContainer.GetAllCookies().FirstOrDefault(cookie => cookie.Name == "session");
        if (session is null)
        {
            return Result.Fail("无法获取Cookie");
        }

        _state = new CoursesZjuState(session);
        SaveState();
        return Result.Success(client);
    }

    private void LoadState()
    {
        _storage.TryGetWithEncryption(StateKey, out _state);
    }

    private void SaveState()
    {
        if (_state is null)
        {
            _storage.Remove(StateKey);
            return;
        }

        _storage.SetWithEncryption(StateKey, _state);
    }

    public Task InitializeAsync(IServiceProvider serviceProvider)
    {
        LoadState();
        return Task.CompletedTask;
    }
}
