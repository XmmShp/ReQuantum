namespace ReQuantum.Contract.Calendar;

/// <summary>
/// 待办事项
/// </summary>
public record CalendarTodo(
    long Id,
    string Content,
    DateTime DueTime,
    bool IsCompleted,
    DateTime CreatedAt,
    Dictionary<string, object?> Properties);
