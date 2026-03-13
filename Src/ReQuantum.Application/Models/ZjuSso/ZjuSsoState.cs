using System.Net;

namespace ReQuantum.Application.Models.ZjuSso;

public record ZjuSsoState
{
    public string? UserName { get; init; }

    public string? LoginName { get; init; }

    public string? UserId { get; init; }

    public Cookie IPlanetDirectoryPro { get; init; } = default!;
}
