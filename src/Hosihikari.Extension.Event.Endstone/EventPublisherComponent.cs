using Hosihikari.Binding;
using Hosihikari.Binding.Endstone;
using Hosihikari.Carrier;
using Hosihikari.Carrier.Endstone;
using Hosihikari.Extension.Abstractions;
using InteropPlugin = Hosihikari.Binding.Endstone.Interop.endstone.Plugin;
using Microsoft.Extensions.Logging;
using ContractChat = Hosihikari.Extension.Event.PlayerChatEvent;
using ContractJoin = Hosihikari.Extension.Event.PlayerJoinEvent;
using ContractLeave = Hosihikari.Extension.Event.PlayerLeaveEvent;
using NativeEvent = Hosihikari.Binding.Endstone.Event;

namespace Hosihikari.Extension.Event.Endstone;

/// <summary>Feeds the carrier's player events into the framework bus.</summary>
/// <remarks>
/// The carrier package injects the host plugin handle; without it the component starts with no
/// listeners instead of failing the boot. There is no per-subscription unregister call: stopping
/// drops the managed delegates, and the carrier removes the native handlers when it disables the
/// plugin.
/// <para>
/// Endstone rejects listener registration until the host plugin is enabled, and only logs the
/// rejection — so the listeners are registered through a <see cref="PluginEnableGate"/> and the
/// capability is marked available only afterwards. Marking it earlier would tell plugins events
/// are coming when nothing is attached.
/// </para>
/// </remarks>
public sealed class EventPublisherComponent : ICarrierComponent
{
    private readonly List<EventSubscription> _subscriptions = [];
    private IEventBus? _bus;
    private BindingScope? _scope;
    private Plugin? _plugin;
    private ILogger? _logger;
    private PluginEnableGate? _gate;

    /// <inheritdoc/>
    public ValueTask StartAsync(ICarrierComponentContext context, CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (_subscriptions.Count != 0 || _gate is not null)
        {
            throw new InvalidOperationException("The event publisher is already started.");
        }

        if (context.CarrierServices.GetService(typeof(ICarrierHost)) is not ICarrierHost { Plugin: not 0 } host)
        {
            return ValueTask.CompletedTask;
        }

        var scope = BindingScope.For(typeof(EventPublisherComponent).Assembly);
        _scope = scope;
        _plugin = Plugin.From(InteropPlugin.Borrow(host.Plugin), scope);
        _bus = context.Events;
        _logger = context.Logger;
        _gate = PluginEnableGate.RunWhenEnabled(_plugin, Register, _logger);
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc/>
    public ValueTask StopAsync(CancellationToken token)
    {
        _gate?.Dispose();
        _gate = null;

        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
        _bus = null;
        _scope = null;
        _plugin = null;
        _logger = null;
        return ValueTask.CompletedTask;
    }

    /// <summary>Registers the native listeners; runs on the server thread after enable.</summary>
    private void Register()
    {
        var plugin = _plugin;
        var bus = _bus;
        var scope = _scope;
        if (plugin is null || bus is null || scope is null)
        {
            return;
        }

        try
        {
            _subscriptions.Add(EventBus.Register(plugin, "PlayerJoinEvent", OnJoin, scope: scope));
            _subscriptions.Add(EventBus.Register(plugin, "PlayerQuitEvent", OnQuit, scope: scope));
            _subscriptions.Add(EventBus.Register(plugin, "PlayerChatEvent", OnChat, scope: scope));
            EventExtension.MarkAvailable();
        }
        catch (Exception exception)
        {
            _logger?.LogError(exception, "The event listeners could not be registered.");
            foreach (var subscription in _subscriptions)
            {
                subscription.Dispose();
            }

            _subscriptions.Clear();
        }
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
