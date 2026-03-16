using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Services.ZjuSso;

namespace ReQuantum.Application.Services.CoursesZju;

[AutoInject(Lifetime.Singleton)]
public class CoursesZjuTodoAfterReadyHandler : IZjuLoginAfterReadyHandler
{
    private const string TodoApi = "https://courses.zju.edu.cn/api/todos?no-intercept=true";

    public Task<Result> OnAfterReadyAsync(IZjuLoginAfterReadyContext context, CancellationToken cancellationToken = default)
    {
        return context.VisitAsync(TodoApi, cancellationToken);
    }
}
