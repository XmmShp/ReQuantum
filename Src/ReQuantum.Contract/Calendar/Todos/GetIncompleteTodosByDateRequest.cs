using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 获取指定日期之后的未完成待办
/// </summary>
public record GetIncompleteTodosByDateRequest(DateOnly Date) : IRequest<GetIncompleteTodosByDateResponse>;

public record GetIncompleteTodosByDateResponse(List<CalendarTodo> Todos);
