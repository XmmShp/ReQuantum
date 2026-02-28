using ReQuantum.Application.Models.Zdbk;
using ReQuantum.Shared.Models;

namespace ReQuantum.Application.Services.Zdbk;

public interface IAcademicCalendarService
{
    Task<Result<AcademicCalendar>> GetCurrentCalendarAsync();
    Task<Result<AcademicCalendar>> RefreshCalendarAsync();
    AcademicCalendar? GetCachedCalendar();
}
