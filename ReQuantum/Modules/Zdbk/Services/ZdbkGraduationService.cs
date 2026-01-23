using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.ZjuSso.Services;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Zdbk.Services;

public interface IZdbkGraduationService
{
    Task<Result<string>> GetGraduationAuditInfoAsync();
}

[AutoInject(Lifetime.Singleton)]
public class ZdbkGraduationService : IZdbkGraduationService
{
    private readonly IZjuSsoService _zjuSsoService;
    private readonly ILogger<ZdbkGraduationService> _logger;

    public ZdbkGraduationService(IZjuSsoService zjuSsoService, ILogger<ZdbkGraduationService> logger)
    {
        _zjuSsoService = zjuSsoService;
        _logger = logger;
    }

    public async Task<Result<string>> GetGraduationAuditInfoAsync()
    {
        // Placeholder
        return Result.Fail("Not implemented yet");
    }
}
