using NOF.Contract;
using ReQuantum.Application.Zdbk.Models;

namespace ReQuantum.Application.Zdbk.Services;

public interface IZdbkSectionScheduleService
{
    Task<Result<ZdbkSectionScheduleResponse>> GetCourseScheduleAsync(string academicYear, string semester);
    Task<Result<ZdbkSectionScheduleResponse>> GetCurrentSemesterScheduleAsync();
}
