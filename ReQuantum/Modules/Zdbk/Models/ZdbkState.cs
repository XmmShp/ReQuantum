using System.Net;

namespace ReQuantum.Modules.Zdbk.Models;

/// <summary>
/// 教务网认证状态
/// </summary>
public record ZdbkState(Cookie SessionCookie, Cookie RouteCookie);
