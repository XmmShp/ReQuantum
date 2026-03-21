using NOF.Application;
using NOF.Contract;
using ReQuantum.Application.ZjuSso.Services;
using ReQuantum.Contract.ZjuSso;

namespace ReQuantum.Application.ZjuSso.RequestHandlers;

public class LogoutZju(IZjuAuthAccessor authAccessor) : IRequestHandler<LogoutZjuRequest>
{
    public Task<Result> HandleAsync(LogoutZjuRequest request, CancellationToken cancellationToken)
    {
        authAccessor.Clear();
        return Task.FromResult(Result.Success());
    }
}
