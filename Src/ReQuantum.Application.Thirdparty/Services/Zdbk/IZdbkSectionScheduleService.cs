using NOF.Contract;
using ReQuantum.Application.Models.Zdbk;

namespace ReQuantum.Application.Services.Zdbk;

public interface IZdbkSectionScheduleService
{
    Task<Result<ZdbkSectionScheduleResponse>> GetCourseScheduleAsync(string academicYear, string semester);
    Task<Result<ZdbkSectionScheduleResponse>> GetCurrentSemesterScheduleAsync();
}
