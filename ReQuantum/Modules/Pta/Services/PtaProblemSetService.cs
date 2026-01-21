using Microsoft.Extensions.Logging;
using ReQuantum.Assets.I18n;
using ReQuantum.Infrastructure.Abstractions;
using ReQuantum.Infrastructure.Models;
using ReQuantum.Infrastructure.Services;
using ReQuantum.Modules.Common.Attributes;
using ReQuantum.Modules.Pta.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace ReQuantum.Modules.Pta.Services;

public interface IPtaProblemSetService
{
    Task<Result<List<PtaProblemSet>>> GetProblemSetsAsync();
}

[AutoInject(Lifetime.Singleton)]
public class PtaProblemSetService : IPtaProblemSetService
{
    private readonly IPtaAuthService _ptaAuthService;
    private readonly ILogger<PtaProblemSetService> _logger;
    private readonly ILocalizer _localizer;

    private const string ProblemSetsApiUrl = "https://pintia.cn/api/problem-sets?filter=%7B%7D&page=0&limit=30&order_by=END_AT&asc=false";

    public PtaProblemSetService(IPtaAuthService ptaAuthService, ILogger<PtaProblemSetService> logger, ILocalizer localizer)
    {
        _ptaAuthService = ptaAuthService;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<Result<List<PtaProblemSet>>> GetProblemSetsAsync()
    {
        var clientResult = await _ptaAuthService.GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail(clientResult.Message);
        }

        var client = clientResult.Value;

        try
        {
            var response = await client.GetAsync(ProblemSetsApiUrl);

            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail($"{_localizer[nameof(UIText.GetProblemSetFailed)]}: {response.StatusCode}");
            }

            var problemSetsResponse = await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.PtaProblemSetsResponse);

            if (problemSetsResponse is null)
            {
                return Result.Fail(_localizer[nameof(UIText.ParseProblemSetFailed)]);
            }

            // 只返回 30 天内的习题集（包括已过期但在 30 天内的）
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var activeProblemSets = problemSetsResponse.ProblemSets
                .Where(ps => ps.EndAt > thirtyDaysAgo)
                .ToList();

            return Result.Success(activeProblemSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 PTA 习题集时发生异常");
            return Result.Fail($"{_localizer[nameof(UIText.GetProblemSetException)]}: {ex.Message}");
        }
    }
}
