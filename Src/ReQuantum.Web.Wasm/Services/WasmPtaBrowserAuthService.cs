using NOF.Contract;
using ReQuantum.Application.Services.Pta;
using ReQuantum.Shared.Services;

namespace ReQuantum.Web.Wasm.Services;

public class WasmPtaBrowserAuthService : PtaBrowserAuthService
{
    public WasmPtaBrowserAuthService(IStorage storage) : base(storage)
    {
    }

    public override Task<Result> OpenBrowserAndWaitForLoginAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(Result.Fail("500", "浏览器登录在 WebAssembly 环境中不可用，请使用手动 Cookie 登录"));
    }
}
