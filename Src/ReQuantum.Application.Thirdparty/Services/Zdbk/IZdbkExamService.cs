using NOF.Contract;
using ReQuantum.Application.Models.Zdbk;

namespace ReQuantum.Application.Services.Zdbk;

public interface IZdbkExamService
{
    Task<Result<List<ParsedExamInfo>>> GetExamsAsync();
}
