using Hosihikari.Binding;
using Hosihikari.Binding.Endstone;
using Hosihikari.Carrier.Endstone;
using Hosihikari.Extension.Abstractions;
using InteropPlugin = Hosihikari.Bindings.Endstone.Interop.endstone.Plugin;
using ContractChat = Hosihikari.Extension.Event.PlayerChatEvent;
using ContractJoin = Hosihikari.Extension.Event.PlayerJoinEvent;
using ContractLeave = Hosihikari.Extension.Event.PlayerLeaveEvent;
using NativeEvent = Hosihikari.Binding.Endstone.Event;

namespace Hosihikari.Extension.Event.Endstone;

/// <summary>Feeds the carrier's player events into the framework bus.</summary>
/// <remarks>
/// The carrier package injects the host plugin handle; without it the component starts with no
/// listeners instead of failing the boot. The carrier has no unregister call, so stopping only
/// drops the managed delegates.
/// </remarks>
public sealed class EventPublisherComponent : ICarrierComponent
{
    private readonly List<EventSubscription> _subscriptions = [];
    private IEventBus? _bus;

    /// <inheritdoc/>
    public ValueTask StartAsync(ICarrierComponentContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_subscriptions.Count != 0)
        {
            throw new InvalidOperationException("The event publisher is already started.");
        }

        if (context.CarrierServices.GetService(typeof(ICarrierHost)) is not ICarrierHost { Plugin: not 0 } host)
        {
            return ValueTask.CompletedTask;
        }

        var scope = BindingScope.For(typeof(EventPublisherComponent).Assembly);
        var plugin = Plugin.From(InteropPlugin.Borrow(host.Plugin), scope);
        _bus = context.Events;
        _subscriptions.Add(EventBus.Register(plugin, "PlayerJoinEvent", OnJoin, scope: scope));
        _subscriptions.Add(EventBus.Register(plugin, "PlayerQuitEvent", OnQuit, scope: scope));
        _subscriptions.Add(EventBus.Register(plugin, "PlayerChatEvent", OnChat, scope: scope));
        EventExtension.MarkAvailable();
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken token)
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _bus = null;
        return ValueTask.CompletedTask;
    }

    private void OnJoin(NativeEvent native)
    {
        if (native.AsPlayerJoin() is not { Player: { } player })
        {
            return;
        }

        _bus?.Publish(new ContractJoin(new EndstonePlayerView(player)));
    }

    private void OnQuit(NativeEvent native)
    {
        if (native.AsPlayerQuit() is not { Player: { } player })
        {
            return;
        }

        _bus?.Publish(new ContractLeave(new EndstonePlayerView(player)));
    }

    private void OnChat(NativeEvent native)
    {
        if (native.AsPlayerChat() is not { Player: { } player } chat)
        {
            return;
        }

        var contract = new ContractChat(new EndstonePlayerView(player), chat.Message);
        _bus?.Publish(contract);
        if (contract.Message != chat.Message)
        {
            chat.Message = contract.Message;
        }

        if (contract.IsCancelled)
        {
            chat.Cancel();
        }
    }
}

internal sealed class EndstonePlayerView(Player player) : IPlayerView
{
    public string Name => player.Name;

    public string Xuid => player.Xuid;

    public object NativeView => player;
}
