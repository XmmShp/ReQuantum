using System.Net;

namespace ReQuantum.Application.Zdbk.Models;

public record ZdbkState(Cookie SessionCookie, Cookie RouteCookie);
