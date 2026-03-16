using NOF.Contract;
using ReQuantum.Application.Models.ZjuSso;
using ReQuantum.Application.Services.ZjuSso;
using ReQuantum.Shared.Services;

namespace ReQuantum.Web.Wasm.Services;

public class WasmZjuContext : ZjuContext, IZjuAuthenticator, IZjuLoginStateWriter
{
    public WasmZjuContext(IStorage storage) : base(storage)
    {
    }

    public override Task<Result> AcquireSessionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(Result.Fail("500", "浏览器登录在 WebAssembly 环境中不可用，请使用手动 Cookie 登录"));
    }

    public Task<Result> AuthorizeAsync(HttpClient client, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(Result.Fail("500", "WebAssembly 环境中未提供 ZJU HTTP 鉴权实现"));
    }

    public Task<Result> SetAuthenticatedStateAsync(string cookieValue, ZjuLoginInfo loginInfo, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return SetAuthenticatedStateAsync(new System.Net.Cookie("iPlanetDirectoryPro", cookieValue, "/", "zju.edu.cn"), loginInfo);
    }
}
