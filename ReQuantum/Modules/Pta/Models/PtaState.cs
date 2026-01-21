using System.Net;

namespace ReQuantum.Modules.Pta.Models;

public record PtaState(string Email, string Password, Cookie PTASessionCookie);
