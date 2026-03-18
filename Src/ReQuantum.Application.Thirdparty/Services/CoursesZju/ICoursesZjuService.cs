using NOF.Contract;
using ReQuantum.Application.Models.CoursesZju;

namespace ReQuantum.Application.Services.CoursesZju;

public interface ICoursesZjuService
{
    Task<Result<HashSet<CoursesZjuTodoDto>>> GetTodoListAsync();
}
