using ReQuantum.Application.Models.CoursesZju;
using ReQuantum.Shared.Models;

namespace ReQuantum.Application.Services.CoursesZju;

public interface ICoursesZjuService
{
    Task<Result<HashSet<CoursesZjuTodoDto>>> GetTodoListAsync();
}
