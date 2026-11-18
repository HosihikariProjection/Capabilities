# Hosihikari Projection — 能力扩展

[English](README.md) | [简体中文](README.zh_hans.md)

面向插件作者的载体无关能力。插件按 `Hosihikari.Extension.*` 契约定型写一次，不在代码里指名载体；绑定面仍按载体分开（Design ADR-0002），本层在其上做能力抹平（Design ADR-0028）。

| 包 | 形态 | 版本 |
|---|---|---|
| `Hosihikari.Extension.Abstractions` | 共享契约（`IPlayerView` 等） | 0.1.0 |
| `Hosihikari.Extension.Event` | 事件目录与事件能力 | 0.1.0 |
| `Hosihikari.Extension.Event.Endstone` | Endstone 适配器 | 跟随 Endstone Idiomatic 版本 |
| `Hosihikari.Extension.Event.LeviLamina` | LeviLamina 适配器与载体组件 | 26.60.7-r1 |
| `Hosihikari.Extension.Hook` | Hook 能力（Paper 式运行期挂钩） | 0.1.0 |
| `Hosihikari.Extension.Hook.LeviLamina` | LeviLamina Hook 适配器 | 跟随 LeviLamina Idiomatic 版本 |
| `Hosihikari.Extension.UI` | UI 能力（表单） | 0.1.0 |
| `Hosihikari.Extension.UI.Endstone` | Endstone 表单适配器 | 跟随 Endstone Idiomatic 版本 |
| `Hosihikari.Carrier.LeviLamina` | LeviLamina 载体组件（服务端线程上下文） | 26.60.7-r1 |
| `Hosihikari.Carrier.Endstone` | Endstone 载体组件与宿主句柄契约 | 0.11.15-r0 |
| `Hosihikari.Extension.Async` | Async 能力（服务端线程调度） | 0.1.0 |
| `Hosihikari.Extension.Async.LeviLamina` | LeviLamina Async 适配器 | 26.60.7-r1 |
| `Hosihikari.Extension.Async.Endstone` | Endstone Async 适配器与载体组件 | 跟随 Endstone Idiomatic 版本 |

`Abstractions` 与 `Event` 是共享层程序集：框架总线按 `typeof(TEvent)` 精确匹配，所有插件必须看到同一份事件类型。载体声明它们，发布器作为进程级**载体组件**运行（框架 ADR-0061）；插件只通过 `IPluginContext.Events` 订阅。

契约包走自己的 SemVer；适配器与所适配载体的 Idiomatic 包锁步。

## 状态

契约与事件目录是第一个切片。Endstone 发布器组件已写好，等载体包注入宿主句柄后挂接（v2 可部署载体形态，尚无日期）。Hook 能力是 Paper `asm-utils` 的原生对应物：机制可移植，目标按载体与版本；LeviLamina 适配器可用，Endstone 与自持要等 detour 引擎。UI 从 Endstone 的 ActionForm 起步；MessageForm/ModalForm 缺工厂，其余载体需要在报文面上自建表单 JSON。见 [docs/capability-matrix.md](docs/capability-matrix.md)。

`tests/Hosihikari.Extension.Tests` 在没有适配器的进程里验证插件侧表面，这也是能力不可用路径；CI 构建全部工程并运行它。
