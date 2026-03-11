using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 删除待办事项
/// </summary>
public record DeleteTodoRequest(long Id) : IRequest;
