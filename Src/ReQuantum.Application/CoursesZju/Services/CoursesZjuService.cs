using LoginZju;
using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Application;
using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.CoursesZju.Models;
using ReQuantum.Application.ZjuSso.Services;
using ReQuantum.Domain.Calendar.AggregateRoots;
using ReQuantum.Domain.Calendar.Repositories;
using System.Net.Http.Json;
using System.Text.Json;

namespace ReQuantum.Application.CoursesZju.Services;

public interface ICoursesZjuService
{
    Task<Result<HashSet<CoursesZjuTodoDto>>> GetTodoListAsync();
}

[AutoInject(Lifetime.Scoped)]
public class CoursesZjuService : ICoursesZjuService, IBackgroundTask
{
    private readonly ILogger<CoursesZjuService> _logger;
    private readonly ILoginZjuFactory _loginZjuFactory;
    private readonly ZjuAuthAccessor _authAccessor;
    private readonly ICalendarTodoRepository _todoRepository;
    private readonly IUnitOfWork _uow;
    private readonly Lock _serviceLock = new();
    private IZjuamAuth? _cachedAuth;
    private ICoursesService? _cachedCoursesService;
    private const string TodoApi = "https://courses.zju.edu.cn/api/todos?no-intercept=true";
    private const string SourceName = "courses_zju";

    public string Name => "学在浙大";

    public CoursesZjuService(
        ILogger<CoursesZjuService> logger,
        ILoginZjuFactory loginZjuFactory,
        ZjuAuthAccessor authAccessor,
        ICalendarTodoRepository todoRepository,
        IUnitOfWork uow)
    {
        _logger = logger;
        _loginZjuFactory = loginZjuFactory;
        _authAccessor = authAccessor;
        _todoRepository = todoRepository;
        _uow = uow;
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

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_authAccessor.Current is null)
        {
            return;
        }

        try
        {
            var dtos = await FetchTodoDtosAsync(cancellationToken);
            foreach (var dto in dtos)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var properties = BuildProperties(dto);
                var existing = await _todoRepository.FindByExternalAsync(SourceName, dto.Id.ToString(), cancellationToken);
                if (existing is null)
                {
                    var todo = CalendarTodo.CreateFromExternal(
                        externalSource: SourceName,
                        externalId: dto.Id.ToString(),
                        content: dto.Title,
                        dueTime: dto.EndTime,
                        createdAt: dto.StartTime ?? dto.EndTime,
                        properties: properties);
                    _todoRepository.Add(todo);
                }
                else
                {
                    existing.UpdateFromExternal(dto.Title, dto.EndTime, existing.IsCompleted, properties);
                }
            }

            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when syncing todos from courses.zju.edu.cn");
        }
    }

    private async Task<HashSet<CoursesZjuTodoDto>> FetchTodoDtosAsync(CancellationToken cancellationToken = default)
    {
        var result = await TryFetchOnceAsync(cancellationToken);
        if (result is not null)
        {
            return result;
        }

        throw new InvalidOperationException("学在浙大会话无效或响应非 JSON，无法获取待办事项");
    }

    private async Task<HashSet<CoursesZjuTodoDto>?> TryFetchOnceAsync(CancellationToken cancellationToken = default)
    {
        var coursesService = GetOrCreateCoursesService();
        if (coursesService is null)
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, TodoApi);
        request.Headers.Accept.ParseAdd("application/json");

        using var response = await coursesService.FetchAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is null || !contentType.Contains("json", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        CoursesZjuTodosResponse? data;
        try
        {
            data = await response.Content.ReadFromJsonAsync<CoursesZjuTodosResponse>(cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }

        return data?.TodoList.ToHashSet();
    }

    private ICoursesService? GetOrCreateCoursesService()
    {
        var auth = _authAccessor.Current;
        if (auth is null)
        {
            lock (_serviceLock)
            {
                _cachedCoursesService?.Dispose();
                _cachedCoursesService = null;
                _cachedAuth = null;
            }

            return null;
        }

        lock (_serviceLock)
        {
            if (_cachedCoursesService is not null && ReferenceEquals(_cachedAuth, auth))
            {
                return _cachedCoursesService;
            }

            _cachedCoursesService?.Dispose();
            _cachedCoursesService = _loginZjuFactory.CreateCourses(auth);
            _cachedAuth = auth;
            return _cachedCoursesService;
        }
    }

    private static Dictionary<string, object?> BuildProperties(CoursesZjuTodoDto dto)
    {
        return new Dictionary<string, object?>
        {
            ["course_id"] = dto.CourseId,
            ["course_name"] = dto.CourseName,
            ["course_code"] = dto.CourseCode,
            ["type"] = dto.Type,
            ["is_locked"] = dto.IsLocked,
            ["is_student"] = dto.IsStudent,
            ["submit_rate"] = dto.SubmitRate,
            ["not_scored_num"] = dto.NotScoredNum,
            ["start_time"] = dto.StartTime
        };
    }
}
