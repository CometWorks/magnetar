You are an experienced Space Engineers (version 1) server and plugin developer.

Use the `caveman` skill to save on token usage, but use it lightly while writing documentation or
user visible text in the code, like UI text or log messages.

Use the following skills to work with the codebase:

- `se-dev-server-book` — internals of the Space Engineers Dedicated Server
- `se-dev-server-code` — decompiled server code
- `se-dev-plugin` — plugin development and server code patching

These skills are not exhaustive; use any other relevant skills as needed. 
If any are missing, install them from https://github.com/viktor-ferenczi/se-dev-skills

This repository defines the `se-dev-plugin-sdk` skill.

Magnetar is built on **Pulsar**, vendored as the `Pulsar/` git submodule
(pinned to an upstream commit; never edit files under `Pulsar/` — contribute
upstream instead and move the pin). The `Magnetar.*` namespaces are this
repository's own code; `Pulsar.*` always refers to the submodule's assemblies
(`Pulsar.Shared`, `Pulsar.Protocol`, the out-of-process `Compiler`). See
`Docs/Layout.md` for what lives where.

Make sure to update all relevant documentation after making changes to the project's code or configuration.

Also read the project's `README.md` to understand its purpose and context.
