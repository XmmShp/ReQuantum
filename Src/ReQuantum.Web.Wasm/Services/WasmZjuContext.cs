using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.Application.ZjuSso.Services;

namespace ReQuantum.Web.Wasm.Services;

public class WasmZjuContext : ZjuContext
{
    public WasmZjuContext(IStorage storage) : base(storage)
    {
    }

    public override Task<Result> LoginAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(Result.Fail("500", "浏览器登录在 WebAssembly 环境中不可用，请使用手动 Cookie 登录"));
    }
}
