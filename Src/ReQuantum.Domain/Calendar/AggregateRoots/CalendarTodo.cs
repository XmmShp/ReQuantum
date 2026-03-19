using NOF.Domain;

namespace ReQuantum.Domain.Calendar.AggregateRoots;

/// <summary>
/// 待办事项 - 有截止时间和内容
/// </summary>
public class CalendarTodo : AggregateRoot
{
    public CalendarTodoId Id { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime DueTime { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Dictionary<string, object?> Properties { get; private set; } = new();
    /// <summary>外部系统的来源标识，如 "courses_zju"。为 null 时表示本地创建的待办。</summary>
    public string? ExternalSource { get; private set; }
    /// <summary>外部系统中的原始 ID。为 null 时表示本地创建的待办。</summary>
    public string? ExternalId { get; private set; }

    private CalendarTodo() { }

    public static CalendarTodo Create(string content, DateTime dueTime)
    {
        return new CalendarTodo
        {
            Id = CalendarTodoId.New(),
            Content = content,
            DueTime = dueTime,
            IsCompleted = false,
            CreatedAt = DateTime.Now
        };
    }

    public static CalendarTodo CreateFromExternal(
        string externalSource,
        string externalId,
        string content,
        DateTime dueTime,
        DateTime createdAt,
        bool isCompleted = false,
        Dictionary<string, object?>? properties = null)
    {
        return new CalendarTodo
        {
            Id = CalendarTodoId.New(),
            Content = content,
            DueTime = dueTime,
            IsCompleted = isCompleted,
            CreatedAt = createdAt,
            ExternalSource = externalSource,
            ExternalId = externalId,
            Properties = properties ?? new()
        };
    }

    public void Update(string content, DateTime dueTime)
    {
        Content = content;
        DueTime = dueTime;
    }

    public void UpdateFromExternal(string content, DateTime dueTime, bool isCompleted, Dictionary<string, object?>? properties = null)
    {
        Content = content;
        DueTime = dueTime;
        IsCompleted = isCompleted;
        if (properties is not null)
        {
            Properties = properties;
        }
    }

    public void ToggleComplete()
    {
        IsCompleted = !IsCompleted;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }

    public void MarkIncomplete()
    {
        IsCompleted = false;
    }
}
