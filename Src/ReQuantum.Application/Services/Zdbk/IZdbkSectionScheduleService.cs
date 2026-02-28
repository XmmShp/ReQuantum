using ReQuantum.Application.Models.Zdbk;
using ReQuantum.Shared.Models;

namespace ReQuantum.Application.Services.Zdbk;

public interface IZdbkSectionScheduleService
{
    Task<Result<ZdbkSectionScheduleResponse>> GetCourseScheduleAsync(string academicYear, string semester);
    Task<Result<ZdbkSectionScheduleResponse>> GetCurrentSemesterScheduleAsync();
}
