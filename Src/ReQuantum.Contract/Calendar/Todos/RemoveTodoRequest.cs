using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 删除待办事项
/// </summary>
[PublicApi]
public record RemoveTodoRequest(long Id) : IRequest;
