using ReQuantum.Application.Models.Zdbk;
using ReQuantum.Shared.Models;

namespace ReQuantum.Application.Services.Zdbk;

public interface IZdbkExamService
{
    Task<Result<List<ParsedExamInfo>>> GetExamsAsync();
}
