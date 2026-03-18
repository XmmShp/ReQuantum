using Microsoft.Extensions.Logging;
using NOF.Annotation;
using NOF.Contract;
using ReQuantum.Application.Pta.Models;
using System.Net.Http.Json;

namespace ReQuantum.Application.Pta.Services;

[AutoInject(Lifetime.Singleton)]
public class PtaProblemSetService : IPtaProblemSetService
{
    private readonly IPtaBrowserAuthService _ptaAuthService;
    private readonly ILogger<PtaProblemSetService> _logger;

    private const string ProblemSetsApiUrl = "https://pintia.cn/api/problem-sets?filter=%7B%7D&page=0&limit=30&order_by=END_AT&asc=false";

    public PtaProblemSetService(IPtaBrowserAuthService ptaAuthService, ILogger<PtaProblemSetService> logger)
    {
        _ptaAuthService = ptaAuthService;
        _logger = logger;
    }

    public async Task<Result<List<PtaProblemSet>>> GetProblemSetsAsync()
    {
        var clientResult = await _ptaAuthService.GetAuthenticatedClientAsync();
        if (!clientResult.IsSuccess)
        {
            return Result.Fail("400", clientResult.Message);
        }

        var client = clientResult.Value!;

        try
        {
            var response = await client.GetAsync(ProblemSetsApiUrl);
            if (!response.IsSuccessStatusCode)
            {
                return Result.Fail("400", $"获取习题集失败: {response.StatusCode}");
            }

            var problemSetsResponse = await response.Content.ReadFromJsonAsync<PtaProblemSetsResponse>();
            if (problemSetsResponse is null)
            {
                return Result.Fail("400", "解析习题集失败");
            }

            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var activeProblemSets = problemSetsResponse.ProblemSets
                .Where(ps => ps.EndAt > thirtyDaysAgo)
                .ToList();

            return activeProblemSets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取 PTA 习题集时发生异常");
            return Result.Fail("400", $"获取习题集异常: {ex.Message}");
        }
    }
}
