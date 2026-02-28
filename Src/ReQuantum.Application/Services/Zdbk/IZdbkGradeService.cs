using ReQuantum.Application.Models.Zdbk;
using ReQuantum.Shared.Models;

namespace ReQuantum.Application.Services.Zdbk;

public interface IZdbkGradeService
{
    Task<Result<ZdbkGrades>> GetSemesterGradesAsync(string academicYear, string semester);
}
