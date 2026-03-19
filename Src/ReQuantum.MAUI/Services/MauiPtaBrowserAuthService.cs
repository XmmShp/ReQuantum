using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.Pta.Services;

namespace ReQuantum.Infrastructure.Services;

public class MauiPtaBrowserAuthService : PtaBrowserAuthService
{
    public MauiPtaBrowserAuthService(IStorage storage) : base(storage)
    {
    }

    public override Task<Result> OpenBrowserAndWaitForLoginAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("PTA browser login is not yet supported without Playwright.");
    }
}
