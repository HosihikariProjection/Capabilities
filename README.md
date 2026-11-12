# Hosihikari Projection — capability extensions

Carrier-neutral capabilities for plugin authors. A plugin is written once against a
`Hosihikari.Extension.*` contract and does not name the carrier it runs on; the binding
surfaces stay per-carrier (Design ADR-0002), and this layer flattens the capabilities
above them (Design ADR-0028).

| Package | Kind | Version |
|---|---|---|
| `Hosihikari.Extension.Abstractions` | Shared contracts (`IPlayerView`, …) | 0.1.0 |
| `Hosihikari.Extension.Event` | Event catalog and the event extension | 0.1.0 |
| `Hosihikari.Extension.Event.Endstone` | Endstone adapter | follows the Endstone Idiomatic version |

`Abstractions` and `Event` are shared-layer assemblies: the framework bus dispatches by
`typeof(TEvent)`, so every plugin must see the same event type. The carrier package declares
them and hosts the publisher; plugins only subscribe through `IPluginContext.Events`.

Contract packages follow their own SemVer; adapters stay in lock-step with the Idiomatic
package of the carrier they adapt.

## Status

The contracts and the event catalog are the first slice. The Endstone publisher follows once
its event surface is wrapped in `Hosihikari.Binding.Endstone`. See
[docs/capability-matrix.md](docs/capability-matrix.md).
