using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Notes;

/// <summary>
/// 添加便签
/// </summary>
[PublicApi]
public record AddNoteRequest(
    string Content) : IRequest;
