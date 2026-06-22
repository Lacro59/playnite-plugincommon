# playnite-plugincommon — Documentation

Technical documentation for the shared library consumed by Lacro59 [Playnite](https://playnite.link) extensions (CheckDLC, SuccessStory, SystemChecker, and others).

## How to use this library

| Integration                    | Location                                            |
| ------------------------------ | --------------------------------------------------- |
| Git submodule in a plugin repo | `source/playnite-plugincommon/`                     |
| Shared MSBuild items           | `CommonPluginsShared/CommonPluginsShared.projitems` |
| Root overview                  | [../README.md](../README.md)                        |

Plugin projects import `CommonPluginsShared.projitems` (and optionally `CommonPluginsControls`, `CommonPluginsStores`, `CommonPlayniteShared`) into their `.csproj`.

## Library modules (code map)

| Module                   | Path                      | Role                                             | Documented |
| ------------------------ | ------------------------- | ------------------------------------------------ | ---------- |
| `CommonPluginsShared`    | `CommonPluginsShared/`    | Database, controls base, menus, plugin lifecycle | Partial    |
| `CommonPluginsControls`  | `CommonPluginsControls/`  | Shared WPF views and view-models                 | —          |
| `CommonPluginsStores`    | `CommonPluginsStores/`    | Store APIs (Steam, GOG, Epic, …)                 | —          |
| `CommonPluginsResources` | `CommonPluginsResources/` | Localization (`LOC_*`), shared XAML resources    | —          |
| `CommonPlayniteShared`   | `CommonPlayniteShared/`   | Playnite library helpers, manifests              | —          |

Guides in this folder focus on **`CommonPluginsShared`** areas that every data-driven plugin reuses. Additional modules may get their own pages later.

## Guides

### [Plugin user controls](plugin-user-controls.md)

Theme-integrated WPF controls (`PluginUserControlExtendBase` / `PluginUserControlExtend`): game context, debounced updates, cache binding, and post-batch UI refresh.

| Section in guide                                                                       | Summary                                                      |
| -------------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| [Class hierarchy](plugin-user-controls.md#class-hierarchy)                             | `PluginUserControl` → base classes → your control            |
| [How controls are hosted](plugin-user-controls.md#how-controls-are-hosted-in-playnite) | `GetGameViewControl`, XAML root, `OnLoaded`                  |
| [Instance registry](plugin-user-controls.md#instance-registry-and-static-events)       | `WeakReference`, `NotifyAllInstances`, `AttachPluginEvents`  |
| [Update pipeline](plugin-user-controls.md#update-pipeline)                             | Triggers, debounce, `GetOnlyCache` data path                 |
| [Implementing a control](plugin-user-controls.md#implementing-a-new-control)           | Checklist, code skeleton, required vs automatic event wiring |
| [Multi-game batch refresh](plugin-user-controls.md#multi-game-batch-refresh-and-ui)    | `BatchRefreshCompleted`, stale UI fix, API, scope            |
| [Batch data layer](plugin-user-controls.md#batch-refresh-data-layer-context)           | Triple buffering during `Refresh(IEnumerable<Guid>)`         |
| [Debugging tips](plugin-user-controls.md#debugging-tips)                               | Log reasons, DEBUG timers, instance ids                      |

#### Key types

| Type                             | File                                                                |
| -------------------------------- | ------------------------------------------------------------------- |
| `PluginUserControlExtendBase`    | `CommonPluginsShared/Controls/PluginUserControlExtendBase.cs`       |
| `PluginUserControlExtend`        | `CommonPluginsShared/Controls/PluginUserControlExtend.cs`           |
| `PluginDatabaseObject<…>`        | `CommonPluginsShared/Collections/PluginDatabaseObject.cs`           |
| `BatchRefreshCompletedEventArgs` | `CommonPluginsShared/Collections/BatchRefreshCompletedEventArgs.cs` |
| `IPluginDatabase`                | `CommonPluginsShared/Interfaces/IPluginDatabase.cs`                 |

**Recent change (2026-06)** — After a multi-game `Refresh()`, `BatchRefreshCompleted` notifies all living `PluginUserControlExtend` instances to re-read the session cache (no network). Subscription is **centralized in `PluginUserControlExtend`**; plugin controls do not need extra wiring for this event.

## Quick reference — control update triggers

| Event / signal                  | Wired by                             | Per-instance filter                   |
| ------------------------------- | ------------------------------------ | ------------------------------------- |
| `GameContextChanged`            | Playnite SDK                         | Current game                          |
| `DatabaseItemUpdated`           | Plugin `AttachStaticEvents`          | `GameContext.Id` in `updatedIds`      |
| `DatabaseItemCollectionChanged` | Plugin `AttachStaticEvents`          | Any instance with context             |
| `Games.ItemUpdated`             | Base (once globally)                 | Matching `GameContext.Id`             |
| Settings `PropertyChanged`      | Plugin `AttachStaticEvents`          | Activated instances (coalesced 50 ms) |
| **`BatchRefreshCompleted`**     | **`PluginUserControlExtend` (auto)** | **Activated instances with context**  |

## Adding documentation

1. Add a new `.md` file in this `docs/` folder (English, ATX headings).
2. Register it in the [Guides](#guides) section above with a summary table of sections.
3. Link relevant source paths and Playnite SDK tutorials where applicable.
4. Update the [Documentation](../README.md#documentation) line in the root `README.md` if the scope changes significantly.

## External references

- [Playnite 10.x API Reference](https://api.playnite.link/docs/api/index.html)
- [Playnite 10.x Tutorials](https://api.playnite.link/docs/tutorials/index.html)
- [playnite-plugincommon on GitHub](https://github.com/Lacro59/playnite-plugincommon)
