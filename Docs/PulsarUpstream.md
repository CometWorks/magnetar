# Upstream Pulsar proposals

Magnetar's `pulsar-based` branch carries a handful of workarounds for gaps in upstream
[SpaceGT/Pulsar](https://github.com/SpaceGT/Pulsar). This document tracks the proposals
prepared to close those gaps, the text to submit upstream, and the Magnetar code to
delete once each one lands.

The patches live in the local Pulsar checkout at `~/dev/se1/Pulsar`, branch
`magnetar-upstream-fixes`, one commit per proposal on top of upstream `main` (64ad1a1):

| Commit | Proposal |
|---|---|
| `be9f738` | 1. Skip normalizing non-option arguments |
| `9382111` | 2. Support an optional GitHub API token |
| `b2ee39f` | 3. Log and safely auto-answer unattended prompts |
| `3ca7fd2` | 4. Allow hosts to override the stats identity |
| (none)    | 5. Host-agnostic `Patch_Rewriter` (issue only) |

Each commit is independent of the others, so they can be cherry-picked onto separate
branches for individual pull requests. The full solution builds with zero warnings with
all four applied. Proposal texts below are ready to paste into GitHub.

---

## 1. Parser rewrites option values into flags (bug fix PR)

**Commit:** `be9f738` — changes `Shared/Arguments/Parser.cs` only.

**PR title:** Skip normalizing non-option arguments

**PR description:**

> `Parser.Normalize` strips `-` and `/` from every argv token before matching it
> against the option short names, including tokens that are option *values*. A value
> that happens to match a flag name gets rewritten into that flag.
>
> For example, launching with `-profile bare` turns the value `bare` into `-bare`,
> so the profile is lost and core plugin force-loading is disabled instead. The same
> happens with any value named `debug`, `sources`, `continue`, and so on, including
> folder names passed to `-bin64`/`-game2`.
>
> The fix: only tokens starting with `-` or `/` are normalized; everything else
> passes through unchanged. Help/version aliases (`/?`, `-h`, `/v`, ...) all start
> with one of those characters, so they still work.

**Magnetar cleanup once merged:** none required immediately. Magnetar strips
value-taking option pairs from the argv it hands to `Parser.Initialize`
(`ServerFlags.PulsarParserArgs`), which it needs anyway so Pulsar's parser never sees
dedicated-server options. After a submodule bump the stripping becomes belt and
braces rather than a correctness requirement; update the comments in
`Legacy/Arguments/ServerFlags.cs` and `Legacy/Program.cs` that cite the Normalize
bug as the reason.

---

## 2. GitHub token support in the network layer (feature PR)

**Commit:** `9382111` — changes `Shared/Network/GitHub.cs` and
`Shared/Network/NetworkClient.cs`.

**PR title:** Support an optional GitHub API token

**PR description:**

> Pulsar calls the GitHub API anonymously, which is limited to 60 requests per hour
> per IP address. Machines that run many Pulsar-based processes, or share an IP
> (server hosts, CI, VPNs), exhaust that quickly; plugin hash checks and update
> checks then fail until the window resets.
>
> This adds an optional personal access token:
>
> * `GitHub.Token` is a public settable property, defaulting to the
>   `PULSAR_GITHUB_TOKEN` environment variable. No new command line flag; hosts
>   embedding Pulsar can set the property directly.
> * `NetworkClient` attaches it as a `Bearer` header only when the request host is
>   exactly `api.github.com`, `github.com`, or `raw.githubusercontent.com`. It is
>   never sent anywhere else (stats server, NuGet, plugin downloads from other
>   hosts). `HttpClient` drops the `Authorization` header on cross-host redirects,
>   so the `github.com` archive redirect to `codeload.github.com` does not leak it
>   either.
> * With no token set, nothing changes.
>
> A token also lets the same mechanism fetch private repositories, though rate
> limits are the motivating case.

**Magnetar cleanup once merged:** in `Legacy/Program.cs`, replace the "has no
effect" warning for `-github-token` / `MAGNETAR_GITHUB_TOKEN` with
`GitHub.Token = ServerFlags.GitHubToken;` (keep the Magnetar flag and environment
variable names for Quasar compatibility; they just feed the upstream property).
Remove the corresponding "currently inert" notes in the docs and in
`ServerFlags.PrintHelp`.

---

## 3. Headless operation: silent prompts and the update boot-loop (bug fix PR + issue)

**Commit:** `b2ee39f` — changes `Shared/Tools.cs` and `Shared/Updater.cs`.

`PromptResult` defaults to `Cancel` (enum value 0), and under `-noPrompt` the
`InterfaceClient` answers every dialog with an empty response, i.e. `Cancel`,
without logging anything. Three consequences today:

* Every fatal or informative dialog is invisible: nothing reaches the log, so an
  unattended machine exits with no recorded reason (bitrot prompt, launcher
  conflicts, preloader errors).
* `Updater.TryUpdate` treats `Cancel` as "quit": with `-noPrompt` and an update
  available, the process silently calls `Environment.Exit(0)` on every launch.
* `Updater.GameUpdatePrompt` exits unless the answer is `Yes`, so after every game
  update an unattended host boot-loops: it starts, silently exits 0, gets restarted
  by its supervisor, and repeats until a human intervenes.

**PR title:** Log and safely auto-answer unattended prompts

**PR description:**

> With `-noPrompt`, every dialog is answered with the default `PromptResult`
> (`Cancel`) and the message is discarded. That has two bad effects on unattended
> machines: fatal messages never reach the log, and the two update prompts treat
> `Cancel` as "exit the process". A host that restarts Pulsar automatically
> boot-loops after every game update, because `GameUpdatePrompt` exits 0 unless the
> user clicks Yes.
>
> Changes:
>
> * `Tools.ShowMessageBox` now short-circuits under `-noPrompt` (and on interface
>   failure, e.g. the Interface executable missing): it logs the prompt text at a
>   level matching the icon, together with the auto-answer, and returns a fallback
>   result. The default fallback keeps the old mapping (`Ok` for Ok-only prompts,
>   `Cancel` otherwise); callers can pass an explicit `unattendedResult` where
>   `Cancel` is the wrong unattended choice.
> * The Pulsar update prompt auto-answers `No`: skip the update and keep launching,
>   instead of exiting.
> * The game update prompt auto-answers `Yes`: log the notice, clear the plugin
>   caches, and continue, instead of exiting. This removes the boot-loop.
> * All other call sites keep their behavior; the dangerous defaults were confined
>   to `Updater`. Notably the Linux updater's "this folder will be cleaned" prompt
>   still refuses on `Cancel`.
>
> Every auto-answered prompt now leaves a line in `info.log`, so unattended exits
> are diagnosable after the fact.

**Companion issue (larger design, not in the patch):**

> **Title: First-class headless mode**
>
> Running Pulsar-based hosts headless currently relies on a chain of accidents:
> `-noSplash` plus `-noPrompt` plus the Interface executable simply not being
> shipped, with `Tools.ShowMessageBox` swallowing the resulting
> `FileNotFoundException`. It works, but every new `InterfaceClient` call site is a
> potential landmine for embedders.
>
> Suggestion: an explicit no-op interface, either a null-object `InterfaceClient`
> subclass or an `IUserInterface` abstraction that `Tools.EarlyInit` accepts, which
> logs prompts and answers them with per-call defaults (see the auto-answer PR).
> A `-headless` convenience flag could imply `-noSplash -noPrompt` and select it.
> Downstream context: Magnetar (Space Engineers dedicated server host built on
> Pulsar as a submodule) forces `-noSplash -noPrompt -lazySteam` and ships no
> Interface binary; a supported headless mode would replace that arrangement.

**Magnetar cleanup once merged:** `Legacy/Program.cs` can drop its pre-logged
`ShowStartupError` duplicates before `ShowBitrotPrompt` (the prompt text itself is
now logged), and the comment explaining the suppressed `GameUpdatePrompt` handling
in `SetupGameData` can shrink. The manual "log the change and clear caches" branch
becomes redundant with upstream's `Yes` auto-answer; either keep the friendlier
server wording or call the upstream prompt directly.

---

## 4. Injectable stats identity (feature PR)

**Commit:** `3ca7fd2` — changes `Shared/Stats/StatsClient.cs` and
`Shared/Loader.cs`.

**PR title:** Allow hosts to override the stats identity

**PR description:**

> `StatsClient` derives `PlayerHash` from `Steam.GetSteamId()`, which requires an
> initialized Steam *client* API session. Hosts that must not initialize it (a
> dedicated server registers the Steam game-server API in the same process, and
> starting the client API corrupts that registration) cannot use `StatsClient` at
> all, even with a valid consent flow and their own stable identity.
>
> Changes:
>
> * `StatsClient.PlayerHash` gains a public setter. Unset, it lazily computes the
>   hashed Steam ID exactly as before.
> * A `HasIdentity` property (`playerHash is not null || Steam.IsInitialized`)
>   replaces the two `Steam.IsInitialized` guards in `DownloadStats` and
>   `Loader.ReportEnabledPlugins`, so an injected identity enables the same
>   consent-gated stats, tracking and voting paths without touching Steam.
> * Behavior without an injected identity is unchanged.
>
> Downstream context: Magnetar (dedicated server host embedding Pulsar) keeps a
> per-instance random identity on disk and currently duplicates a minimal stats
> client just to substitute it. With this change it would set
> `StatsClient.PlayerHash` at startup and use `StatsClient` directly.

**Magnetar cleanup once merged:** set `StatsClient.PlayerHash =
ConsentManager.PlayerHash` during startup, then delete `Legacy/Stats/VotesClient.cs`
and route `ConsentManager` / `UsageStats` through `StatsClient.Consent` and
`StatsClient.Track`. Note two small behavioral differences to review at that point:
`StatsClient` uses `NetworkClient` with the configured network timeout (VotesClient
uses a private 3-second `HttpClient`), and its request logging differs slightly.

---

## 5. Host-agnostic Patch_Rewriter (issue only, low priority)

No patch. Magnetar originally forked `Legacy/Patch/Patch_Rewriter.cs`; since the
code-linking pass it compiles the file straight from the submodule
(`<Compile Link>` in `Legacy/Legacy.csproj`) together with
`Legacy/Loader/PluginInstance.cs`, which it needs because the rewriter registry is
keyed on the concrete `PluginInstance`. So nothing is broken for Magnetar today;
the coupling just forces any downstream host to compile upstream's `PluginInstance`
(and its dependency closure) even when the host has its own plugin lifecycle types.

**Issue title:** Decouple Patch_Rewriter from PluginInstance

**Issue text:**

> `Patch_Rewriter.Methods` is a `ConcurrentDictionary<PluginInstance, MethodInfo>`,
> and disabling a failed rewriter calls `PluginInstance.ThrowError`. That is the
> only reason the file depends on the loader.
>
> Keying the registry on a small interface instead, say
> `IRewriterOwner { void ThrowError(string message); }` implemented by
> `PluginInstance`, would make the patch self-contained. Hosts that embed Pulsar
> and reuse the compilation-rewrite mechanism could then compile `Patch_Rewriter`
> without also pulling in `PluginInstance`.
>
> Context: Magnetar (dedicated server host using Pulsar as a submodule) compiles
> both files from the submodule today, so this is a nice-to-have for future hosts
> rather than a blocker. Filing it as a design suggestion; happy to send a PR if
> the interface approach is acceptable.

---

## Submitting

Suggested order: 1 (clear bug fix) and 2 (small, self-contained feature) first;
3 next, PR and companion issue together since the issue explains where the design
should eventually go; 4 once there is appetite for stats from non-client hosts;
5 whenever convenient.

To turn a commit into a PR branch:

```bash
cd ~/dev/se1/Pulsar
git switch -c <topic> origin/main
git cherry-pick <commit>
```

After any of these merge upstream, bump the `Pulsar/` submodule in Magnetar and do
the cleanup listed for that proposal, then re-diff the remaining forks under
`Legacy/` against upstream as described in `Docs/Layout.md`.
