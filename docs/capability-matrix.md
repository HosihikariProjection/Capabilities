# Capability matrix

One row per capability, one column per carrier. A cell is either the state of the adapter or
the reason it cannot exist. CI checks that every published adapter has a row entry.

| Capability | Endstone                                                                                                                                                                                                                                    | LeviLamina                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           | Self-hosted Bedrock                                             |
| ---------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------- |
| Event      | Written: publisher component over the loader `EventBus`, one native listener per event type; **needs the host handle** the bootstrap injects (HostingShim #31). Registration visibility for plugins follows the shared-layer decision below | Written but **not buildable yet**: the publisher is in the repository and needs no host handle (the loader bus is a singleton). Registration is fully covered — the final surface (`0f7204f5`) carries `eventId()` on every concrete event _and_ on the 17 event base classes, plus `setMessage` and the cancellable casts. The one gap is identity: `ServerPlayerEvent::self()` returns a bare `nint?` and no player type is projected, so `IPlayerView.Name` cannot be filled. Deferred by decision, not scheduled | Not available: the self-hosted surface has no game event source |
| Command    | Planned                                                                                                                                                                                                                                     | Planned: the surface has no command executor                                                                                                                                                                                                                                                                                                                                                                                                                                                                         | Planned                                                         |
| Async      | Written: `RunAsync`/`Post` and `SwitchToServerThread` over `Scheduler.runTask`, plus the server-thread context. **Needs the host handle**: `Scheduler.runTask` is bound to the host plugin                                                  | Available: `RunAsync`/`Post` and `SwitchToServerThread` over `ServerThreadExecutor`; the server-thread context comes from the carrier component. **Does not need a host handle**                                                                                                                                                                                                                                                                                                                                     | Not available: no general tick hook                             |
| Hook       | Deferred: the runtime's hook is internal, so a detour engine would be needed                                                                                                                                                                | Available: wrapped over `ll::memory::hook` / `unhook`; targets are carrier- and version-specific                                                                                                                                                                                                                                                                                                                                                                                                                     | Deferred: needs a detour engine and a tick target               |
| UI         | Available: action forms; message/modal forms need factories                                                                                                                                                                                 | Planned: would build form JSON over the Bedrock packet surface                                                                                                                                                                                                                                                                                                                                                                                                                                                       | Planned: same packet path                                       |

**Host handle**: only the Endstone components depend on `ICarrierHost`; the LeviLamina ones
never read it (the registered handle there is zero by design). The bootstrap package is what
loads the carrier's binding shim from the carrier directory, so a missing shim fails the whole
boot with a legible reason instead of surfacing at a component's first binding call.

**Availability is late and not monotonic within a plugin's own startup**: Endstone rejects
listener registration and scheduler submissions until the host plugin is enabled, so a
component that installs on the carrier side becomes available only after that. Endstone enables
child plugins _before_ the carrier plugin, so a plugin's `OnEnable` can read
`EventExtension.IsAvailable == false` and see `true` later; read it at the point of use rather
than caching it. Subscribing does not need the flag — subscriptions made before the publisher
attaches start receiving events once it does.

**Plugin visibility**: components live on the unloadable carrier side, so what they register is
only visible to plugins if the contract assemblies are in the carrier's shared layer. That is a
packaging decision recorded in the carrier package manifest; until it lands, a written component
is not the same as a reachable capability.

**Listener lifetime on Endstone**: there is no per-subscription unregister call. Disposing a
subscription drops the managed delegate; the native handler is removed by the carrier when it
disables the owning plugin (`EndstonePluginManager::disablePlugin` → `HandlerList::unregister`),
which also runs the release pointer and frees the callback state. So a disable/reload cycle
neither leaves stale handlers nor delivers an event twice, and stopping the component early is
an optimization rather than a leak requirement.

**What has been verified, and what has not**: the Endstone package has been booted on BDS
26.60.6 with the carrier plugin, the bootstrap, the shared layer and a probe plugin; the
"register while not enabled" rejection is gone and the publisher reports available only after
all three listeners register. **Actual delivery from a native event into a plugin's
`Subscribe<T>` has not been observed yet** — it needs a logged-in client, and the event
constructors cannot synthesize a player without one (`PlayerChatEvent::Create` is a stub for the
`std::vector<Player*>` parameter; join/quit take a real `Player`).

An unavailable capability throws `CarrierCapabilityUnavailableException` from its entry point
and reports `IsAvailable = false`; it never degrades silently.
