# Upstream Pulsar proposals

Magnetar's `pulsar-based` branch carries a handful of workarounds for gaps in upstream
[SpaceGT/Pulsar](https://github.com/SpaceGT/Pulsar). This document tracks the proposals
prepared to close those gaps, the text submitted upstream, and the Magnetar code to
delete once each one lands.

Proposals 1–3 merged upstream as a single squashed commit,
[`b9f391e`](https://github.com/SpaceGT/Pulsar/commit/b9f391eef89463bdbe6d8067081512746674f196)
("Magnetar upstream fixes (#49)"), and the `Pulsar/` submodule now points at it. The
Magnetar-side cleanup for those three is done; what upstream actually merged differs
from the proposal text in two places, noted below.

| Proposal | Status |
|---|---|
| 1. Skip normalizing non-option arguments | Merged in `b9f391e`, as proposed |
| 2. Support an optional GitHub API token | Merged in `b9f391e`, different mechanism |
| 3. Log and safely auto-answer unattended prompts | Merged in `b9f391e`, partially |
| 4. Allow hosts to override the stats identity | Open — patch `3ca7fd2` |
| 5. Host-agnostic `Patch_Rewriter` (issue only) | Open — no patch |

The remaining patch lives in the local Pulsar checkout at `~/dev/se1/Pulsar`, branch
`magnetar-upstream-fixes`. Proposal texts below are ready to paste into GitHub.

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

**Magnetar cleanup (done):** the stripping in `ServerFlags.PulsarParserArgs` stays.
Magnetar needs it anyway so Pulsar's parser never sees dedicated-server options, and
it is still load-bearing for one case the upstream fix does not cover: a `/`-rooted
value. `Normalize` skips tokens that start with neither `-` nor `/`, so `-path debug`
is now safe, but on Linux `-path /debug` still trims to `debug` and would flip
Pulsar's `-debug`. The comments in `Legacy/Arguments/ServerFlags.cs` and
`Legacy/Program.cs` were rewritten to state that narrower reason.

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

**What actually merged:** the token property and the `Bearer` header landed as
proposed, but the host allow-list did not. Instead of accepting `api.github.com`,
`github.com` and `raw.githubusercontent.com`, upstream moved the archive and file
endpoints onto the API host — `FetchRepo` is now `/repos/{0}/zipball/{1}` and
`FetchFile` is `/repos/{0}/contents/{1}?ref={2}` with an
`Accept: application/vnd.github.raw+json` header — and `IsTokenHost` allow-lists
`api.github.com` alone. The effect is the same and private repositories work, but
every GitHub call now counts against the API rate limit, including the raw file
fetches that used to be unmetered. That makes the token more valuable to set, not
less.

**Magnetar cleanup (done):** Magnetar carries no token code at all. Its own
`-github-token` flag and `MAGNETAR_GITHUB_TOKEN` variable were dropped in 2.1.0
in favour of Pulsar's `PULSAR_GITHUB_TOKEN`, which `GitHub.Token` reads on its
own; the flag is still stripped from the command line for one release, with a
deprecation warning. Following upstream in offering no CLI flag is also the
safer choice, since `/proc/<pid>/cmdline` is world-readable on Linux.

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

**What actually merged:** the dangerous exits are fixed, the logging design is not.
Upstream put the log line in `InterfaceClient.ShowPrompt` — a single
`LogFile.Warn($"Prompt cancelled: {message}")` when `-noPrompt` is set — rather than
in `Tools.ShowMessageBox`. There is no `unattendedResult` parameter and no
icon-matched log level, so a fatal prompt and an informational one look the same in
the log, and a caller cannot pick a per-call fallback. `Updater.TryUpdate` no longer
exits on `Cancel` and its prompt dropped to `YesNo`; `GameUpdatePrompt` exits only on
an explicit `No`, so under `-noPrompt` (which answers `Cancel`) it now continues and
clears the plugin caches. The boot-loop is gone.

The companion "first-class headless mode" issue still stands, and is arguably more
relevant now: the auto-answer behaviour is spread across `InterfaceClient` and
`Updater` rather than centralised.

**Magnetar cleanup (done):** less than the proposal anticipated. The
`ShowStartupError` calls before `ShowBitrotPrompt` are **not** duplicates — they carry
different, more specific text than Pulsar's generic "You have a broken Pulsar
insallation!", and they also write to `Console.Error`, which the upstream `LogFile.Warn`
does not. They stay. `SetupGameData` likewise keeps its own branch: upstream's prompt
text is written for the game client (Discord, Plugin Hub snapshots, "click Yes to
continue") and would be logged verbatim on a server. Only the comments changed, to
stop citing an exit path that no longer exists.

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

Proposals 1–3 are merged. Remaining: 4 once there is appetite for stats from
non-client hosts, and 5 whenever convenient. The companion issue for 3 (first-class
headless mode) has not been filed yet.

To turn a commit into a PR branch:

```bash
cd ~/dev/se1/Pulsar
git switch -c <topic> origin/main
git cherry-pick <commit>
```

After any of these merge upstream, bump the `Pulsar/` submodule in Magnetar and do
the cleanup listed for that proposal, then re-diff the remaining forks under
`Legacy/` against upstream as described in `Docs/Layout.md`.
