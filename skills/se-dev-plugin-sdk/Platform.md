# Platform Compilation Symbols

Magnetar compiles plugin source with a preprocessor symbol that names the
operating system the server is **currently running on**, so a plugin can pick
between Windows- and Linux-specific code paths at compile time.

| Platform | Symbol defined |
|---|---|
| **Windows** | `PLATFORM_WINDOWS` |
| **Linux** | `PLATFORM_LINUX` |

Exactly one of the two is defined for any given compilation. The symbol is set
by the host when it builds the plugin — you do not declare it yourself and you
do not need any `.csproj` configuration for it.

> This is **not** part of `PluginSdk` itself; it is a guarantee made by the
> Magnetar compiler. It exists so plugins can tell which platform they are on
> and therefore which SDK behaviour to expect.

Running on Proton/Wine is considered as running on Windows.

## Usage

```csharp
#if PLATFORM_LINUX
    // Linux-only code: case-sensitive filesystem, no Windows API, etc.
#elif PLATFORM_WINDOWS
    // Windows-only code.
#endif
```

Prefer writing platform-agnostic code against the SDK facades (`PathResolver`,
`Logger`, `ServerControl`) — they already absorb the platform differences.
Reach for the symbols only when you genuinely need a different code path that
the facades do not cover, such as P/Invoke into OS-specific libraries.

## Notes

- The symbol reflects the **runtime** OS of the dedicated server, not the build
  machine. Plugins are compiled on the server that loads them.
- Only `PLATFORM_WINDOWS` and `PLATFORM_LINUX` are defined; there is no symbol
  for other operating systems.
- The host also defines the usual target-framework symbols (`NETFRAMEWORK` or
  `NETCOREAPP`, plus `TRACE` and, in debug builds, `DEBUG`). On the legacy
  .NET Framework path the platform is always `PLATFORM_WINDOWS`.
