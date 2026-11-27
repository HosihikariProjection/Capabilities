using Hosihikari.Binding;
using Hosihikari.Binding.Abi;
using Hosihikari.Binding.LeviLamina;
using Hosihikari.Carrier;
using Hosihikari.Extension.Abstractions;
using ContractChat = Hosihikari.Extension.Event.PlayerChatEvent;
using ContractJoin = Hosihikari.Extension.Event.PlayerJoinEvent;
using ContractLeave = Hosihikari.Extension.Event.PlayerLeaveEvent;
using InteropBus = Hosihikari.Binding.LeviLamina.Interop.ll.@event.EventBus;
using InteropChat = Hosihikari.Binding.LeviLamina.Interop.ll.@event.PlayerChatEvent;
using InteropDisconnect = Hosihikari.Binding.LeviLamina.Interop.ll.@event.PlayerDisconnectEvent;
using InteropEvent = Hosihikari.Binding.LeviLamina.Interop.ll.@event.Event;
using InteropEventIdView = Hosihikari.Binding.LeviLamina.Interop.ll.@event.EventIdView;
using InteropJoin = Hosihikari.Binding.LeviLamina.Interop.ll.@event.PlayerJoinEvent;
using InteropNativeMod = Hosihikari.Binding.LeviLamina.Interop.ll.mod.NativeMod;
using InteropServerPlayer = Hosihikari.Binding.LeviLamina.Interop.ServerPlayer;

namespace Hosihikari.Extension.Event.LeviLamina;

/// <summary>Feeds the loader's player events into the framework bus.</summary>
/// <remarks>
/// One native listener per event type; the listener is an <c>ll::event::ListenerBase</c> director
/// whose <c>call</c> forwards the raw <c>Event*</c> back here. Every listener carries the carrier
/// mod's handle — the loader locks it before accepting a registration — and the component drops
/// them all on stop. Without a handle the component starts with no listeners rather than
/// failing the boot.
/// </remarks>
public sealed class EventPublisherComponent : ICarrierComponent
{
    private readonly List<Listener> _listeners = [];
    private IEventBus? _bus;
    private EventBus? _nativeBus;
    private SharedHandle? _mod;

    /// <inheritdoc/>
    public ValueTask StartAsync(ICarrierComponentContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_bus is not null)
        {
            throw new InvalidOperationException("The event publisher is already started.");
        }

        if (
            context.CarrierServices.GetService(typeof(ICarrierHost))
            is not ICarrierHost { Plugin: not 0 } host
        )
        {
            return ValueTask.CompletedTask;
        }

        var native = EventBus.Instance;
        var scope = BindingScope.For(typeof(EventPublisherComponent).Assembly);
        // The carrier owns the mod; the resolve adds one shared reference of our own, held for
        // as long as any listener names it.
        var mod = ResolveCarrierMod(host.Plugin);
        _mod = mod;
        _bus = context.Events;
        _nativeBus = native;

        var (joinName, _) = InteropJoin.eventId();
        _listeners.Add(Attach(joinName, scope, mod));
        var (disconnectName, _) = InteropDisconnect.eventId();
        _listeners.Add(Attach(disconnectName, scope, mod));
        var (chatName, _) = InteropChat.eventId();
        _listeners.Add(Attach(chatName, scope, mod));

        EventExtension.MarkAvailable();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken token)
    {
        foreach (var listener in _listeners)
        {
            listener.Dispose();
        }

        _listeners.Clear();
        // After every director is released: the loader only held the mod through them.
        _mod?.Dispose();
        _mod = null;
        _nativeBus = null;
        _bus = null;
        return ValueTask.CompletedTask;
    }

    /// <summary>The carrier mod, as a reference the listener base can lock.</summary>
    /// <remarks>
    /// <c>ListenerBase</c> stores a <c>weak_ptr&lt;Mod&gt;</c>. Resolving it by name would be the
    /// obvious route, but the registry cannot answer here: the mod is only added to it after the
    /// loader's load callback returns, and a carrier component starts inside that callback.
    /// </remarks>
    private static SharedHandle ResolveCarrierMod(nint hostPlugin)
    {
        // The host handle is a module handle, not a NativeMod*: `Borrow` would only reinterpret
        // it, the module lookup is what maps it to the object.
        //
        // No registry round-trip: `addMod`/`handleMap` are written in the `onLoad().transform`
        // continuation, which runs after `load` returns, and components start inside `load`.
        return InteropNativeMod.current(hostPlugin)
            ?? throw new InvalidOperationException("The carrier mod could not be resolved.");
    }

    private Listener Attach(string eventName, BindingScope scope, SharedHandle mod)
    {
        var id =
            InteropEventIdView.Create(eventName)
            ?? throw new InvalidOperationException(
                $"Could not create the event id for {eventName}."
            );

        var listener = new Listener(this, id, mod);
        listener.Attach(_nativeBus!);
        scope.Track(listener);
        return listener;
    }

    /// <summary>Dispatches one raw event into the bus.</summary>
    internal void Dispatch(InteropEvent native)
    {
        var bus = _bus;
        if (bus is null)
        {
            return;
        }

        if (native.TryAsPlayerJoinEvent() is { } join)
        {
            if (join.AsServerPlayerEvent()?.self() is { } player)
            {
                bus.Publish(new ContractJoin(new LeviLaminaPlayerView(player)));
            }

            return;
        }

        if (native.TryAsPlayerDisconnectEvent() is { } disconnect)
        {
            if (disconnect.AsServerPlayerEvent()?.self() is { } player)
            {
                bus.Publish(new ContractLeave(new LeviLaminaPlayerView(player)));
            }

            return;
        }

        if (native.TryAsPlayerChatEvent() is { } chat)
        {
            DispatchChat(bus, chat);
        }
    }

    private static void DispatchChat(IEventBus bus, InteropChat chat)
    {
        if (chat.AsServerPlayerEvent()?.self() is not { } player)
        {
            return;
        }

        var sent = chat.message();
        var contract = new ContractChat(new LeviLaminaPlayerView(player), sent);
        bus.Publish(contract);
        if (contract.Message != sent)
        {
            chat.setMessage(contract.Message);
        }

        if (contract.IsCancelled)
        {
            chat.AsCancellable_ll__event__ServerPlayerEvent_()?.cancel();
        }
    }
}

/// <summary>A player as the carrier sees it, valid only during the callback.</summary>
/// <remarks>
/// <c>ServerPlayerEvent::self()</c> answers a borrowed <c>ServerPlayer</c>, live only for the
/// callback. The identity members sit on its bases; <c>getNameTag()</c> on <c>Actor</c> is not
/// exported, so the name is reached through <c>AsPlayer()</c> instead.
/// </remarks>
internal sealed class LeviLaminaPlayerView(InteropServerPlayer player) : IPlayerView
{
    // getNameTag() sits on Actor and is not exported, so the name comes from the Player-level
    // equivalent, which is. Resolved once: the downcast is a shim call, and the event is going
    // to be read more than once per dispatch.
    public string Name { get; } = player.AsPlayer()?.getRealName() ?? "";

    // getXuid() is a pure virtual with no exported symbol; `_getXuid` is the virtual mirror the
    // surface emits for it (the `$` spelling, made identifier-safe).
    public string Xuid { get; } = player.AsPlayer()?._getXuid() ?? "";

    public object NativeView => player;
}
