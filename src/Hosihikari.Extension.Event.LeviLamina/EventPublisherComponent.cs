using Hosihikari.Binding;
using Hosihikari.Binding.LeviLamina;
using Hosihikari.Extension.Abstractions;
using InteropBus = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.EventBus;
using InteropChat = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.PlayerChatEvent;
using InteropDisconnect = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.PlayerDisconnectEvent;
using InteropEvent = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.Event;
using InteropEventIdView = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.EventIdView;
using InteropJoin = Hosihikari.Bindings.LeviLamina.Interop.ll.@event.PlayerJoinEvent;
using InteropServerPlayer = Hosihikari.Bindings.LeviLamina.Interop.ServerPlayer;
using ContractChat = Hosihikari.Extension.Event.PlayerChatEvent;
using ContractJoin = Hosihikari.Extension.Event.PlayerJoinEvent;
using ContractLeave = Hosihikari.Extension.Event.PlayerLeaveEvent;

namespace Hosihikari.Extension.Event.LeviLamina;

/// <summary>Feeds the loader's player events into the framework bus.</summary>
/// <remarks>
/// One native listener per event type; the listener is an <c>ll::event::ListenerBase</c> director
/// whose <c>call</c> forwards the raw <c>Event*</c> back here. The component owns the bus
/// reference and drops every listener on stop — the listeners carry no mod pointer, so nothing
/// else would remove them.
/// </remarks>
public sealed class EventPublisherComponent : ICarrierComponent
{
    private readonly List<Listener> _listeners = [];
    private IEventBus? _bus;
    private EventBus? _nativeBus;

    /// <inheritdoc/>
    public ValueTask StartAsync(ICarrierComponentContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_bus is not null)
        {
            throw new InvalidOperationException("The event publisher is already started.");
        }

        var native = EventBus.Instance;
        var scope = BindingScope.For(typeof(EventPublisherComponent).Assembly);
        _bus = context.Events;
        _nativeBus = native;

        var (joinName, _) = InteropJoin.eventId();
        _listeners.Add(Attach(joinName, scope));
        var (disconnectName, _) = InteropDisconnect.eventId();
        _listeners.Add(Attach(disconnectName, scope));
        var (chatName, _) = InteropChat.eventId();
        _listeners.Add(Attach(chatName, scope));

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
        _nativeBus = null;
        _bus = null;
        return ValueTask.CompletedTask;
    }

    private Listener Attach(string eventName, BindingScope scope)
    {
        var id = InteropEventIdView.Create(eventName)
            ?? throw new InvalidOperationException($"Could not create the event id for {eventName}.");

        var listener = new Listener(this, id);
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
/// The generated surface is flat — <c>ServerPlayer</c> does not inherit <c>Player</c>/<c>Actor</c>
/// — so identity members are reached through the emitted downcasts.
/// </remarks>
internal sealed class LeviLaminaPlayerView(InteropServerPlayer player) : IPlayerView
{
    public string Name => player.AsActor()?.getNameTag() ?? "";

    // The carrier's getXuid() is a pure virtual with no exported symbol.
    public string Xuid => "";

    public object? NativeView => player;

    /// <summary>The native UUID text, or an empty string once the player is gone.</summary>
    public string Uuid => player.AsPlayer()?.getUuid()?.asString() ?? "";
}
