using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ReQuantum.Application.Models.CoursesZju;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Models;
using ReQuantum.Shared.Services;

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
            return Result<HashSet<CoursesZjuTodoDto>>.Failure(clientResult.Message);

        var client = clientResult.Value!;
        var result = await client.GetAsync(TodoApi);

        if (!result.IsSuccessStatusCode)
        {
            _state = null;
            return Result<HashSet<CoursesZjuTodoDto>>.Failure($"获取待办事项失败: {result.StatusCode}");
        }

        try
        {
            var response = await result.Content.ReadFromJsonAsync<CoursesZjuTodosResponse>();
            if (response is null)
                return Result<HashSet<CoursesZjuTodoDto>>.Failure("解析待办事项失败");

            return response.TodoList.ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when getting todo list from courses.zju.edu.cn");
            return Result<HashSet<CoursesZjuTodoDto>>.Failure($"获取待办事项失败：{ex.Message}");
        }
    }

    private async Task<Result<RequestClient>> GetAuthenticatedClient()
    {
        if (_state is not null)
            return Result<RequestClient>.Success(RequestClient.Create(new RequestOptions { Cookies = [_state.Session] }));

        var clientResult = await _zjuSsoService.GetAuthenticatedClientAsync(new RequestOptions { AllowRedirects = true });
        if (!clientResult.IsSuccess)
            return Result<RequestClient>.Failure(clientResult.Message);

        var client = clientResult.Value!;
        await client.GetAsync(TodoApi);
        var session = client.CookieContainer.GetAllCookies().FirstOrDefault(cookie => cookie.Name == "session");
        if (session is null)
            return Result<RequestClient>.Failure("无法获取Cookie");

        _state = new CoursesZjuState(session);
        SaveState();
        return Result<RequestClient>.Success(client);
    }

    private void LoadState() => _storage.TryGet(StateKey, out _state);

    private void SaveState()
    {
        if (_state is null) { _storage.Remove(StateKey); return; }
        _storage.Set(StateKey, _state);
    }
}
