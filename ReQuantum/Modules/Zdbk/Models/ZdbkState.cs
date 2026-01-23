using System.Net;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 教务网认证状态
/// </summary>
public record ZdbkState(
    Cookie SessionCookie,
    Cookie RouteCookie,
    string? StudentId = null,
    string? StudentName = null,
    string? Grade = null,
    string? Major = null,
    string? AdministrativeClass = null,
    string? College = null,
    string? AcademicYear = null,
    string? Semester = null
);
