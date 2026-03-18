using ReQuantum.Contract.Calendar;

namespace ReQuantum.Application.Services;

/// <summary>
/// 外部待办来源提供者，可将第三方数据源的待办映射为 <see cref="CalendarTodo"/>。
/// </summary>
public interface ICalendarTodoProvider
{
    /// <summary>提供者名称，用于标识来源。</summary>
    string Name { get; }

    /// <summary>
    /// 获取指定日期范围内的待办事项。
    /// </summary>
    IAsyncEnumerable<CalendarTodo> GetTodosAsync(DateOnly start, DateOnly end, CancellationToken cancellationToken = default);
}
