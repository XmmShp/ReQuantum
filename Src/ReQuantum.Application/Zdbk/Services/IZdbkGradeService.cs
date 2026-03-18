using NOF.Contract;
using ReQuantum.Application.Zdbk.Models;

namespace ReQuantum.Application.Zdbk.Services;

public interface IZdbkGradeService
{
    Task<Result<ZdbkGrades>> GetSemesterGradesAsync(string academicYear, string semester);
}
