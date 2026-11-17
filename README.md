# Hosihikari Projection — capability extensions

[English](README.md) | [简体中文](README.zh_hans.md)

Carrier-neutral capabilities for plugin authors. A plugin is written once against a
`Hosihikari.Extension.*` contract and does not name the carrier it runs on; the binding
surfaces stay per-carrier (Design ADR-0002), and this layer flattens the capabilities
above them (Design ADR-0028).

| Package | Kind | Version |
|---|---|---|
| `Hosihikari.Extension.Abstractions` | Shared contracts (`IPlayerView`, …) | 0.1.0 |
| `Hosihikari.Extension.Event` | Event catalog and the event extension | 0.1.0 |
| `Hosihikari.Extension.Event.Endstone` | Endstone adapter | follows the Endstone Idiomatic version |
| `Hosihikari.Extension.Hook` | Hook capability (Paper-style runtime hooking) | 0.1.0 |
| `Hosihikari.Extension.Hook.LeviLamina` | LeviLamina hook adapter | follows the LeviLamina Idiomatic version |
| `Hosihikari.Extension.UI` | UI capability (forms) | 0.1.0 |
| `Hosihikari.Extension.UI.Endstone` | Endstone form adapter | follows the Endstone Idiomatic version |
| `Hosihikari.Carrier.LeviLamina` | LeviLamina carrier component (server-thread context) | 26.60.7-r0 |
| `Hosihikari.Carrier.Endstone` | Endstone carrier components and the host-handle contract | 0.11.15-r0 |
| `Hosihikari.Extension.Async` | Async capability (server-thread scheduling) | 0.1.0 |
| `Hosihikari.Extension.Async.LeviLamina` | LeviLamina async adapter | 26.60.7-r0 |
| `Hosihikari.Extension.Async.Endstone` | Endstone async adapter and component | follows the Endstone Idiomatic version |

`Abstractions` and `Event` are shared-layer assemblies: the framework bus dispatches by
`typeof(TEvent)`, so every plugin must see the same event type. The carrier declares them and
the publisher runs as a process-wide carrier component (framework ADR-0061); plugins only
subscribe through `IPluginContext.Events`.

Contract packages follow their own SemVer; adapters stay in lock-step with the Idiomatic
package of the carrier they adapt.

## Status

The contracts and the event catalog are the first slice. The Endstone publisher component is
written; it attaches once the carrier package injects the host handle (the v2 deployable
carrier shape, which has no date yet). The hook capability is the native analogue of Paper's
asm-utils: the mechanism is portable, the targets are carrier- and version-specific. The
LeviLamina adapter is available; Endstone and self-hosted wait for a detour engine. UI starts
with action forms on Endstone; message and modal forms need factories, and the other carriers
would build form JSON over the packet surface. See
[docs/capability-matrix.md](docs/capability-matrix.md).

`tests/Hosihikari.Extension.Tests` exercises the plugin-facing surface without an adapter,
which is also the capability-unavailable path; CI builds every project and runs it.
