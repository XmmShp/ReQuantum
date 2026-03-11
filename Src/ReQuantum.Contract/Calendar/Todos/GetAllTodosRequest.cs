using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 获取所有待办事项
/// </summary>
public record GetAllTodosRequest : IRequest<GetAllTodosResponse>;

public record GetAllTodosResponse(List<CalendarTodo> Todos);
