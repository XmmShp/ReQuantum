using NOF.Contract;
using ReQuantum.Application.Pta.Models;

namespace ReQuantum.Application.Pta.Services;

public interface IPtaProblemSetService
{
    Task<Result<List<PtaProblemSet>>> GetProblemSetsAsync();
}
