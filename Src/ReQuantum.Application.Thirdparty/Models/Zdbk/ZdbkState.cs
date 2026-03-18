using System.Net;

namespace ReQuantum.Application.Models.Zdbk;

public record ZdbkState(Cookie SessionCookie, Cookie RouteCookie);
