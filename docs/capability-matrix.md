# Capability matrix

One row per capability, one column per carrier. A cell is either the state of the adapter or
the reason it cannot exist. CI checks that every published adapter has a row entry.

| Capability | Endstone | LeviLamina | Self-hosted Bedrock |
|---|---|---|---|
| Event | Available: publisher component written; waits for the carrier package to inject the host handle (HostingShim #31, no date yet) | Planned: needs the `ServerPlayer` identity surface | Not available: the self-hosted surface has no game event source |
| Command | Planned | Planned: the surface has no command executor | Planned |
| Async | Available: `RunAsync`/`Post` and `SwitchToServerThread` over `Scheduler.runTask`; ambient `await` continuations need the carrier component, which waits for the host handle (HostingShim #31, no date yet) | Available: `RunAsync`/`Post` and `SwitchToServerThread` over `ServerThreadExecutor`; ambient `await` continuations need the carrier component, which has no production driver yet | Not available: no general tick hook |
| Hook | Deferred: the runtime's hook is internal, so a detour engine would be needed | Available: wrapped over `ll::memory::hook` / `unhook`; targets are carrier- and version-specific | Deferred: needs a detour engine and a tick target |
| UI | Available: action forms; message/modal forms need factories | Planned: would build form JSON over the Bedrock packet surface | Planned: same packet path |

An unavailable capability throws `CarrierCapabilityUnavailableException` from its entry point
and reports `IsAvailable = false`; it never degrades silently.
