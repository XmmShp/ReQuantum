using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Models.CoursesZju;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;
using System.Net;
using System.Net.Http.Json;

namespace ReQuantum.Application.Services.CoursesZju;

[AutoInject(Lifetime.Singleton)]
public class CoursesZjuService : ICoursesZjuService
{
    private readonly IZjuContext _zjuContext;
    private readonly IStorage _storage;
    private readonly ILogger<CoursesZjuService> _logger;
    private CoursesZjuState? _state;
    private const string StateKey = "CoursesZju:State";
    private const string TodoApi = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

    public CoursesZjuService(IZjuContext zjuContext, IStorage storage, ILogger<CoursesZjuService> logger)
    {
        _zjuContext = zjuContext;
        _storage = storage;
        _logger = logger;
        _zjuContext.OnLogout += () => _state = null;
    }

    public async Task<Result<HashSet<CoursesZjuTodoDto>>> GetTodoListAsync()
    {
        var clientResult = await GetAuthenticatedClient();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail("400", clientResult.Message);
        }

        var client = clientResult.Value!;
        var result = await client.GetAsync(TodoApi);

        if (!result.IsSuccessStatusCode)
        {
            _state = null;
            return Result.Fail("400", $"获取待办事项失败: {result.StatusCode}");
        }

        try
        {
            var response = await result.Content.ReadFromJsonAsync<CoursesZjuTodosResponse>();
            if (response is null)
            {
                return Result.Fail("400", "解析待办事项失败");
            }

            return response.TodoList.ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when getting todo list from courses.zju.edu.cn");
            return Result.Fail("400", $"获取待办事项失败：{ex.Message}");
        }
    }

    private async Task<Result<HttpClient>> GetAuthenticatedClient()
    {
        await LoadStateAsync();

        if (_state is not null)
        {
            return HttpClientUtilities.Create(new RequestOptions { Cookies = [_state.Session] });
        }

        var authResult = await _zjuContext.EnsureAuthenticatedAsync();
        if (!authResult.IsSuccess)
        {
            return Result.Fail("400", authResult.Message);
        }

        if (!_zjuContext.IsAuthenticated || _zjuContext.SessionCookie is null)
        {
            return Result.Fail("400", "未登录");
        }

        var cookies = new List<Cookie> { _zjuContext.SessionCookie };
        using var client = HttpClientUtilities.Create(new RequestOptions
        {
            AllowRedirects = false,
            Cookies = [_zjuContext.SessionCookie]
        });
        using var response = await HttpClientUtilities.GetWithCookieTrackingAsync(client, TodoApi, cookies);
        var session = cookies.FirstOrDefault(cookie => cookie.Name == "session");
        if (session is null)
        {
            return Result.Fail("400", "无法获取Cookie");
        }

        _state = new CoursesZjuState(session);
        await SaveStateAsync();
        return HttpClientUtilities.Create(new RequestOptions { Cookies = [session] });
    }

    private async ValueTask LoadStateAsync()
    {
        if (_state is not null)
        {
            return;
        }

        var state = await _storage.TryGetAsync<CoursesZjuState>(StateKey);
        _state = state.ValueOr((CoursesZjuState?)null);
    }

    private async ValueTask SaveStateAsync()
    {
        if (_state is null)
        {
            await _storage.RemoveAsync(StateKey);
            return;
        }

        await _storage.SetAsync(StateKey, _state);
    }
}
