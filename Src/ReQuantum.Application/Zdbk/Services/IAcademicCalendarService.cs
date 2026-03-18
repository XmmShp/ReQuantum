using NOF.Contract;
using ReQuantum.Application.Zdbk.Models;

namespace ReQuantum.Application.Zdbk.Services;

public interface IAcademicCalendarService
{
    Task<Result<AcademicCalendar>> GetCurrentCalendarAsync();
    Task<Result<AcademicCalendar>> RefreshCalendarAsync();
    AcademicCalendar? GetCachedCalendar();
}
