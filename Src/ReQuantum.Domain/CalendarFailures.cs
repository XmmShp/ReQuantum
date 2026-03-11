using NOF.Domain;

namespace ReQuantum.Contract.Calendar;

/// <summary>
/// 日历模块失败定义
/// </summary>
[Failure("EventNotFound", "日程事件不存在。", "404001")]
[Failure("TodoNotFound", "待办事项不存在。", "404002")]
[Failure("NoteNotFound", "便签不存在。", "404003")]
[Failure("InvalidDateRange", "无效的日期范围。", "400001")]
public static partial class CalendarFailures;
