using NOF.Contract;
using ReQuantum.Application.ZjuSso.Models;

namespace ReQuantum.Contract.ZjuSso;

[PublicApi]
public record LoginZjuRequest(string Username, string Password) : IRequest<ZjuLoginInfo>;
