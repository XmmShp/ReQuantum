using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ReQuantum.Application.Common.Services;

namespace ReQuantum.Services;

/// <summary>
/// 后台任务宿主服务，定期调度所有注册的 <see cref="IBackgroundTask"/> 实现。
/// </summary>
public class BackgroundTaskHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackgroundTaskHostedService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(15);

    public BackgroundTaskHostedService(IServiceProvider serviceProvider, ILogger<BackgroundTaskHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await RunAllTasksAsync(stoppingToken);
            await Task.Delay(Interval, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
        }
    }

    private async Task RunAllTasksAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var tasks = scope.ServiceProvider.GetServices<IBackgroundTask>();
        foreach (var task in tasks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            try
            {
                _logger.LogInformation("Running background task: {Name}", task.Name);
                await task.ExecuteAsync(cancellationToken);
                _logger.LogInformation("Background task completed: {Name}", task.Name);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background task failed: {Name}", task.Name);
            }
        }
    }
}
