using NOF.Contract;
using ReQuantum.Application.Models.Pta;

namespace ReQuantum.Application.Services.Pta;

public interface IPtaProblemSetService
{
    Task<Result<List<PtaProblemSet>>> GetProblemSetsAsync();
}
