# Capability matrix

One row per capability, one column per carrier. A cell is either the state of the adapter or
the reason it cannot exist. CI checks that every published adapter has a row entry.

| Capability | Endstone | LeviLamina | Self-hosted Bedrock |
|---|---|---|---|
| Event | First slice: Join / Leave / Chat | Planned: needs the `ServerPlayer` identity surface and the mutable-string setter | Not available: the self-hosted surface has no game event source |
| Command | Planned | Planned: the surface has no command executor | Planned |
| Async | Planned: server-thread `SynchronizationContext` over `Scheduler.runTask` | Planned: over `ServerThreadExecutor.getDefault()` | Not available: no general tick hook |
| Hook | Deferred: the runtime's hook is internal, so a detour engine would be needed | Available as an escape hatch over `ll::memory::hook` / `unhook`; targets stay carrier- and version-specific | Deferred: needs a detour engine and a tick target |
| UI | Later | Later | Later |

An unavailable capability throws `CarrierCapabilityUnavailableException` from its entry point
and reports `IsAvailable = false`; it never degrades silently.
