namespace ReQuantum.Domain.Calendar;

/// <summary>
/// 待办事项 - 有截止时间和内容
/// </summary>
public class CalendarTodo
{
    public CalendarTodoId Id { get; private set; }
    public int TodoId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTime DueTime { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public Dictionary<string, object?> Properties { get; private set; } = new();

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

    public void Update(string content, DateTime dueTime)
    {
        Content = content;
        DueTime = dueTime;
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
