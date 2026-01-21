using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace ReQuantum.Modules.Calendar.Entities;

/// <summary>
/// 待办 - 有截止时间和内容
/// </summary>
public partial class CalendarTodo : ObservableObject
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime DueTime { get; set; } // 截止时间（包含日期和时间）

    [ObservableProperty]
    private bool _isCompleted;

    public DateTime CreatedAt { get; set; }

    public Dictionary<string, object?> Properties { get; init; }

    public CalendarTodo()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.Now;
        DueTime = DateTime.Now;
    }
}
