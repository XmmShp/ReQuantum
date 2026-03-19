namespace ReQuantum.Application.Common.Services;

/// <summary>
/// 后台任务抽象，由应用层定义，由宿主层定期调度执行。
/// </summary>
public interface IBackgroundTask
{
    /// <summary>任务名称，用于日志标识。</summary>
    string Name { get; }

    /// <summary>
    /// 执行一次后台任务。
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}
