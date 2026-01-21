using Microsoft.Extensions.Logging;
using ReQuantum.Infrastructure.Models;
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

    private const string ProblemSetsApiUrl = "https://pintia.cn/api/problem-sets?filter=%7B%7D&page=0&limit=30&order_by=END_AT&asc=false";

    public PtaProblemSetService(IPtaAuthService ptaAuthService, ILogger<PtaProblemSetService> logger)
    {
        _ptaAuthService = ptaAuthService;
        _logger = logger;
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
                return Result.Fail($"获取习题集失败: {response.StatusCode}");
            }

            var problemSetsResponse = await response.Content.ReadFromJsonAsync(SourceGenerationContext.Default.PtaProblemSetsResponse);

            if (problemSetsResponse is null)
            {
                return Result.Fail("解析习题集响应失败");
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
            return Result.Fail($"获取习题集异常: {ex.Message}");
        }
    }
}
