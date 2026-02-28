using Microsoft.Extensions.Logging;
using NOF.Contract;
using ReQuantum.Application.Models.CoursesZju;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;
using System.Net.Http.Json;

namespace ReQuantum.Application.Services.CoursesZju;

public class CoursesZjuService : ICoursesZjuService
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
        LoadState();
    }

    public async Task<Result<HashSet<CoursesZjuTodoDto>>> GetTodoListAsync()
    {
        var clientResult = await GetAuthenticatedClient();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(400, clientResult.Message);
        }

        var client = clientResult.Value!;
        var result = await client.GetAsync(TodoApi);

        if (!result.IsSuccessStatusCode)
        {
            _state = null;
            return Result.Fail(400, $"获取待办事项失败: {result.StatusCode}");
        }

        try
        {
            var response = await result.Content.ReadFromJsonAsync<CoursesZjuTodosResponse>();
            if (response is null)
            {
                return Result.Fail(400, "解析待办事项失败");
            }

            return response.TodoList.ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when getting todo list from courses.zju.edu.cn");
            return Result.Fail(400, $"获取待办事项失败：{ex.Message}");
        }
    }

    private async Task<Result<RequestClient>> GetAuthenticatedClient()
    {
        if (_state is not null)
        {
            return RequestClient.Create(new RequestOptions { Cookies = [_state.Session] });
        }

        var clientResult = await _zjuSsoService.GetAuthenticatedClientAsync(new RequestOptions { AllowRedirects = true });
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(400, clientResult.Message);
        }

        var client = clientResult.Value!;
        await client.GetAsync(TodoApi);
        var session = client.CookieContainer.GetAllCookies().FirstOrDefault(cookie => cookie.Name == "session");
        if (session is null)
        {
            return Result.Fail(400, "无法获取Cookie");
        }

        _state = new CoursesZjuState(session);
        SaveState();
        return client;
    }

    private void LoadState() => _storage.TryGet(StateKey, out _state);

    private void SaveState()
    {
        if (_state is null)
        { _storage.Remove(StateKey); return; }
        _storage.Set(StateKey, _state);
    }
}
