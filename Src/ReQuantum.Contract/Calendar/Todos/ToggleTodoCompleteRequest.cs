using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Todos;

/// <summary>
/// 切换待办完成状态
/// </summary>
[PublicApi]
public record ToggleTodoCompleteRequest(long Id) : IRequest;
