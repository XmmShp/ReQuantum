using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.ZjuSso.Abstractions;

namespace ReQuantum.Application.CoursesZju.Services;

[AutoInject(Lifetime.Singleton)]
public class CoursesZjuSessionAfterReadyHandler : IZjuLoginAfterReadyHandler
{
    private const string TodoApi = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

    public Task<Result> OnAfterReadyAsync(IZjuLoginAfterReadyContext context, CancellationToken cancellationToken = default)
    {
        return context.VisitAsync(TodoApi, cancellationToken);
    }
}
