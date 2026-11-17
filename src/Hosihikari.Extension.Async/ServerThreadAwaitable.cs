using System.Runtime.CompilerServices;

namespace Hosihikari.Extension.Async;

/// <summary>An awaitable that resumes the continuation on the server thread.</summary>
public readonly struct ServerThreadAwaitable
{
    /// <summary>Gets the awaiter.</summary>
    public Awaiter GetAwaiter() => default;

    /// <summary>Posts the continuation to the server thread.</summary>
    public readonly struct Awaiter : INotifyCompletion
    {
        /// <summary>Always yields; the continuation must be posted.</summary>
        public bool IsCompleted => false;

        /// <inheritdoc/>
        public void OnCompleted(Action continuation) => AsyncExtension.Service.Post(continuation);

        /// <summary>Nothing to observe.</summary>
        public void GetResult()
        {
        }
    }
}
