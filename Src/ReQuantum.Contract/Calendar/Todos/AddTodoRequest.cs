using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 添加待办事项
/// </summary>
[PublicApi]
public record AddTodoRequest(
    string Content,
    DateTime DueTime,
    bool IsCompleted) : IRequest;
