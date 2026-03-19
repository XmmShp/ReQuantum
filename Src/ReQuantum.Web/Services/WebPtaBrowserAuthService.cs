using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.Pta.Services;

namespace ReQuantum.Web.Services;

public class WebPtaBrowserAuthService : PtaBrowserAuthService
{
    public WebPtaBrowserAuthService(IStorage storage) : base(storage)
    {
    }

    public override Task<Result> OpenBrowserAndWaitForLoginAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("PTA browser login is not yet supported without Playwright.");
    }
}
