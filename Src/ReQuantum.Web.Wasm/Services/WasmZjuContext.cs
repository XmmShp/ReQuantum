using NOF.Contract;
using ReQuantum.Application.Common.Services;
using ReQuantum.UI.Services;

namespace ReQuantum.Web.Wasm.Services;

public class WasmZjuContext : ZjuContext
{
    public WasmZjuContext(IStorage storage, IEncryptor encryptor) : base(storage, encryptor)
    {
    }

    public override Task<Result> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(Result.Fail("500", "登录在 WebAssembly 环境中不可用，请使用手动登录"));
    }
}
