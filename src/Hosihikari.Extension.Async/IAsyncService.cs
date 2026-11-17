namespace Hosihikari.Extension.Async;

/// <summary>Runs work on the carrier's server thread.</summary>
/// <remarks>
/// Most of the native API may only be called there. <see cref="RunAsync"/> and
/// <see cref="Post"/> always hop to that thread; an <c>await</c> inside a handler that is
/// already on it only returns there when the carrier installed its synchronization context.
/// </remarks>
public interface IAsyncService
{
    /// <summary>Whether the carrier can schedule onto the server thread.</summary>
    bool IsAvailable { get; }

    /// <summary>Whether an ambient synchronization context sends continuations back to the server thread.</summary>
    bool HasSynchronizationContext { get; }

    /// <summary>Queues work to run on the server thread; it is not observed.</summary>
    void Post(Action work);

    /// <summary>Runs work on the server thread and completes there.</summary>
    /// <returns>A task that faults with whatever the work threw.</returns>
    Task RunAsync(Action work);
}
