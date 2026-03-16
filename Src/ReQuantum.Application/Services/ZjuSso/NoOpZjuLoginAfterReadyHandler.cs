using NOF.Annotation;
using NOF.Contract;

namespace ReQuantum.Application.Services.ZjuSso;

[AutoInject(Lifetime.Singleton)]
public class NoOpZjuLoginAfterReadyHandler : IZjuLoginAfterReadyHandler
{
    public Task<Result> OnAfterReadyAsync(IZjuLoginAfterReadyContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Result>(Result.Success());
    }
}
