using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 更新待办事项
/// </summary>
[PublicApi]
public record UpdateTodoRequest(
    long Id,
    string Content,
    DateTime DueTime) : IRequest;
