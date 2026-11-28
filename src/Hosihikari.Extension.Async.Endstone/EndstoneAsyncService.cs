using Hosihikari.Binding;
using Hosihikari.Extension.Abstractions;

namespace Hosihikari.Extension.Async.Endstone;

/// <summary>The async capability over the Endstone scheduler.</summary>
/// <remarks>
/// The executor exists only after the carrier package injects the host plugin, because the
/// scheduler ties queued work to it. Before that — and after the component stops — the service
/// reports unavailable and its calls throw instead of dropping work silently.
/// </remarks>
public sealed class EndstoneAsyncService : IAsyncService
{
    private IServerExecutor? _executor;

    /// <inheritdoc/>
    public bool IsAvailable => Volatile.Read(ref _executor) is not null;

    /// <inheritdoc/>
    public bool HasSynchronizationContext =>
        SynchronizationContext.Current is ServerSynchronizationContext;

    /// <inheritdoc/>
    public void Post(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        Executor.Post(work);
    }

    /// <inheritdoc/>
    public Task RunAsync(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        Executor.Post(() =>
        {
            try
            {
                work();
                completion.SetResult();
            }
            catch (Exception exception)
            {
                completion.SetException(exception);
            }
        });
        return completion.Task;
    }

    internal void SetExecutor(IServerExecutor? executor) => Volatile.Write(ref _executor, executor);

    private IServerExecutor Executor =>
        Volatile.Read(ref _executor)
        ?? throw new CarrierCapabilityUnavailableException("Async", "Endstone");
}
