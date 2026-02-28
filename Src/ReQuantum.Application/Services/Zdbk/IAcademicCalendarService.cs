using NOF.Contract;
using ReQuantum.Application.Models.Zdbk;

namespace ReQuantum.Application.Services.Zdbk;

public interface IAcademicCalendarService
{
    Task<Result<AcademicCalendar>> GetCurrentCalendarAsync();
    Task<Result<AcademicCalendar>> RefreshCalendarAsync();
    AcademicCalendar? GetCachedCalendar();
}
