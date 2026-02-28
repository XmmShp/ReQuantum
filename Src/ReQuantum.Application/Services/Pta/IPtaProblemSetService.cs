using ReQuantum.Application.Models.Pta;
using ReQuantum.Shared.Models;

namespace ReQuantum.Application.Services.Pta;

public interface IPtaProblemSetService
{
    Task<Result<List<PtaProblemSet>>> GetProblemSetsAsync();
}
