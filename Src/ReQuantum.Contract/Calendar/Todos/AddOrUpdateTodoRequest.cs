using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 添加或更新待办事项
/// </summary>
public record AddOrUpdateTodoRequest(
    long? Id,
    string Content,
    DateTime DueTime,
    bool IsCompleted) : IRequest;
