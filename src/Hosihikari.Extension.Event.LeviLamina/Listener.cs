using System.Runtime.InteropServices;
using Hosihikari.Binding.LeviLamina;
using InteropEvent = Hosihikari.Binding.LeviLamina.Interop.ll.@event.Event;
using InteropEventIdView = Hosihikari.Binding.LeviLamina.Interop.ll.@event.EventIdView;
using InteropEventPriority = Hosihikari.Binding.LeviLamina.Interop.ll.@event.EventPriority;
using InteropListenerBase = Hosihikari.Binding.LeviLamina.Interop.ll.@event.ListenerBase;
using SharedHandle = Hosihikari.Binding.Abi.SharedHandle;

namespace Hosihikari.Extension.Event.LeviLamina;

/// <summary>One native listener whose callback lands back on the publisher.</summary>
/// <remarks>
/// The listener carries the carrier mod's handle: LeviLamina locks a listener's owning mod
/// before it accepts a registration, and a director made without one is refused outright.
/// Detaching unregisters from the bus first, then releases the native listener and the managed
/// handle. The event id is owned here and released last.
/// </remarks>
internal sealed unsafe class Listener : IDisposable
{
    private readonly EventPublisherComponent _owner;
    private readonly InteropEventIdView _id;
    private readonly SharedHandle _mod;
    private readonly GCHandle _self;
    private EventBus? _bus;
    private SharedHandle? _director;
    private bool _attached;

    internal Listener(EventPublisherComponent owner, InteropEventIdView id, SharedHandle mod)
    {
        _owner = owner;
        _id = id;
        _mod = mod;
        _self = GCHandle.Alloc(this);
    }

    internal void Attach(EventBus bus)
    {
        if (_attached)
        {
            throw new InvalidOperationException("The listener is already attached.");
        }

        var director =
            InteropListenerBase.CreateDirector(
                (int)InteropEventPriority.Normal,
                _mod,
                (nint)(delegate* unmanaged<nint, nint, void>)&Trampoline.Invoke,
                GCHandle.ToIntPtr(_self),
                (nint)(delegate* unmanaged<nint, void>)&Trampoline.Release
            ) ?? throw new InvalidOperationException("The carrier could not create the listener.");

        if (!bus.AddListener(director, _id))
        {
            director.Dispose();
            throw new InvalidOperationException("The carrier refused the listener.");
        }

        _director = director;
        _bus = bus;
        _attached = true;
    }

    internal void Dispatch(nint eventPointer)
    {
        var native = InteropEvent.Borrow(eventPointer);
        try
        {
            _owner.Dispatch(native);
        }
        finally
        {
            native.Dispose();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_attached)
        {
            _attached = false;
            if (_bus is { } bus && _director is { } director)
            {
                // Unregister first: the callback must not race the handle release.
                bus.RemoveListener(director);
                director.Dispose();
            }

            _bus = null;
            _director = null;
        }

        if (_self.IsAllocated)
        {
            _self.Free();
        }

        _id.Dispose();
    }

    private static class Trampoline
    {
        [UnmanagedCallersOnly]
        internal static void Invoke(nint userData, nint eventPointer)
        {
            if (GCHandle.FromIntPtr(userData).Target is Listener listener)
            {
                listener.Dispatch(eventPointer);
            }
        }

        [UnmanagedCallersOnly]
        internal static void Release(nint userData)
        {
            // The handle is freed by Dispose; the native destructor can run later than the
            // detach, so freeing here would risk a second free.
        }
    }
}
