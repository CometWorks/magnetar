# Repository layout

| Path                         | Purpose                                                           |
| ---------------------------- | ----------------------------------------------------------------- |
| `Legacy/`                    | Launcher (`MagnetarLegacy` / `MagnetarInterim`) — entry point, preloader, patches |
| `ConfigTerminal/`            | `MagnetarConfig` — Terminal.Gui TUI to configure and operate one DS instance (see [ConfigTerminal.md](ConfigTerminal.md)) |
| `ConfigTerminalTests/`       | xUnit tests for `ConfigTerminal` (registry, documents, process/pid, plugins, workshop resolver) |
| `Shared/`                    | Cross-project plugin loader / config / network code               |
| `Compiler/`                  | Roslyn-based on-disk source plugin compiler                       |
| `PluginSdk/`                 | Public API surface plugins compile against                        |
| `MagnetarMod/`               | Companion SE world mod with a standalone MDK2 project for analyzer-backed mod editing |
| `Scripts/`                   | Build helpers (Steamworks.NET, licenses)                          |
| `build/Libraries/`           | Staged Linux dependencies (gitignored, populated by `./build.sh`) |
| `dist/`                      | Packaged distributables (gitignored)                              |

For a deeper, module-by-module / file-by-file tour of the source tree, see the
**[code handbook](TOC.md)**.
