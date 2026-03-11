using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 按日期获取待办事项
/// </summary>
public record GetTodosByDateRequest(DateOnly Date) : IRequest<GetTodosByDateResponse>;

public record GetTodosByDateResponse(List<CalendarTodo> Todos);
