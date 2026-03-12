using NOF.Contract;

namespace ReQuantum.Contract.Calendar.Notes;

/// <summary>
/// 更新便签
/// </summary>
[PublicApi]
public record UpdateNoteRequest(
    long Id,
    string Content) : IRequest;
