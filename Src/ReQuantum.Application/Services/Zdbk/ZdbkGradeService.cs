using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Models.Zdbk;

namespace ReQuantum.Application.Services.Zdbk;

[AutoInject(Lifetime.Singleton)]
public class ZdbkGradeService : IZdbkGradeService
{
    public async Task<Result<ZdbkGrades>> GetSemesterGradesAsync(string academicYear, string semester)
    {
        // TODO: implement real grade fetching
        var result = new ZdbkGrades
        {
            Credit = 18.5,
            MajorCredit = 12.0,
            GradePoint5 = 4.2,
            GradePoint4 = 3.6,
            GradePoint100 = 88.5,
            MajorGradePoint = 4.5,
            CoursesGrade =
            [
                new() { CourseName = "高级程序设计", CourseCode = "CS101", Grade100 = 95, Grade5 = 5.0, Credit = 4.0, Term = "2023-2024-1" },
                new() { CourseName = "线性代数", CourseCode = "MATH102", Grade100 = 82, Grade5 = 3.2, Credit = 3.5, Term = "2023-2024-1" },
                new() { CourseName = "大学物理", CourseCode = "PHYS103", Grade100 = 88, Grade5 = 3.8, Credit = 4.0, Term = "2023-2024-1" },
                new() { CourseName = "思想道德修养与法律基础", CourseCode = "MARX104", Grade100 = 91, Grade5 = 4.1, Credit = 3.0, Term = "2023-2024-1" },
                new() { CourseName = "体育(1)", CourseCode = "PE105", Grade100 = 85, Grade5 = 3.5, Credit = 1.0, Term = "2023-2024-1" }
            ]
        };

        return result;
    }
}
