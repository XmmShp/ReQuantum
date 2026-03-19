namespace ReQuantum.Application.Common.Services;

/// <summary>
/// 应用启动前预热任务抽象，用于从持久化状态恢复运行时内存状态。
/// </summary>
public interface IWarmup
{
    /// <summary>
    /// 执行一次预热任务。
    /// </summary>
    Task WarmupAsync(CancellationToken cancellationToken = default);
}
