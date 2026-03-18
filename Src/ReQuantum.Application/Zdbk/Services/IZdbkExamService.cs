using NOF.Contract;
using ReQuantum.Application.Zdbk.Models;

namespace ReQuantum.Application.Zdbk.Services;

public interface IZdbkExamService
{
    Task<Result<List<ParsedExamInfo>>> GetExamsAsync();
}
