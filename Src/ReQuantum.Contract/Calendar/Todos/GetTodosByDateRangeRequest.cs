using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 按日期范围获取待办事项
/// </summary>
public record GetTodosByDateRangeRequest(DateOnly StartDate, DateOnly EndDate) : IRequest<GetTodosByDateRangeResponse>;

public record GetTodosByDateRangeResponse(List<CalendarTodo> Todos);
