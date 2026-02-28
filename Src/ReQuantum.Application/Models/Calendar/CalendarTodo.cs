namespace ReQuantum.Application.Models.Calendar;

/// <summary>
/// 待办 - 有截止时间和内容
/// </summary>
public class CalendarTodo
{
    public Guid Id { get; set; }
    public int TodoId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime DueTime { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }

    public Dictionary<string, object?> Properties { get; set; } = new();

    public CalendarTodo()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        DueTime = DateTime.Now;
    }
}
