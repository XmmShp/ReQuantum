using NOF.Contract;
using ReQuantum.Application.Models.Zdbk;

namespace ReQuantum.Application.Services.Zdbk;

public interface IZdbkGradeService
{
    Task<Result<ZdbkGrades>> GetSemesterGradesAsync(string academicYear, string semester);
}
