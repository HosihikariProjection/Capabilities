using System.Runtime.CompilerServices;
using Hosihikari.Binding;
using Hosihikari.Binding.LeviLamina;

namespace Hosihikari.Extension.Async.LeviLamina;

/// <summary>The async capability over the loader's server-thread executor.</summary>
public sealed class LeviLaminaAsyncService : IAsyncService
{
    private readonly IServerExecutor _executor = new ServerExecutor();

    /// <inheritdoc/>
    public bool IsAvailable => true;

    /// <inheritdoc/>
    public bool HasSynchronizationContext =>
        SynchronizationContext.Current is ServerSynchronizationContext;

    /// <inheritdoc/>
    public void Post(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        _executor.Post(work);
    }

    /// <inheritdoc/>
    public Task RunAsync(Action work)
    {
        ArgumentNullException.ThrowIfNull(work);
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        _executor.Post(() =>
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
}

internal static class Registration
{
    [ModuleInitializer]
    internal static void Initialize() => AsyncExtension.Use(new LeviLaminaAsyncService());
}
