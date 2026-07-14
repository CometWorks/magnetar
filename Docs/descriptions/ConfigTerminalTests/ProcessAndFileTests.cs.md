# ConfigTerminalTests/ProcessAndFileTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 187

## Summary
xUnit tests for the process/pid/atomic-file and world-creation layer. It verifies the password hash matches the DS PBKDF2 formula, `LastSessionFile` computes a Saves-relative path and serializes the DS `LastSession.sbl` shape, `LaunchSpec` builds the expected launcher args and rejects conflicting extra args, `PidFileReader` distinguishes stale/absent pid states, `AtomicFile` backs up exactly once, and `WorldCreator` copies templates, stamps the world name, synthesizes a config when missing, and rejects existing folders. Uses a per-test temp dir (`IDisposable`).

## Types
### ProcessAndFileTests — class, public, implements `IDisposable`
Each test operates in a fresh temp dir; a private `MakeTemplate` helper builds a minimal DS world template under `Content/CustomWorlds/<name>`.

- **Fields:** `dir` — per-test temp directory.
- **Methods:**
  - `Password_matches_the_DS_pbkdf2_formula()` — `PasswordHasher.Hash` yields a 16-byte salt and 20-byte key; re-deriving with PBKDF2/SHA1, 10000 iters reproduces the hash.
  - `LastSession_computes_relative_path_under_saves()` — `LastSessionFile.ForWorld` sets `RelativePath`/`GameName`; `Write` emits `<RelativePath>`, `<MyObjectBuilder_LastSession`, and `<IsContentWorlds>false</IsContentWorlds>`.
  - `LaunchSpec_builds_expected_args_and_rejects_conflicts()` — `BuildArgs` includes `-daemon`, `-path /data`, `-config /cfg`, `-ignorelastsession`; `RejectionReason` flags `-ignorelastsession`/`-session:` in `ExtraArgs` but allows `-noconsent`.
  - `PidFileReader_reports_stale_for_a_dead_pid()` — a pid file for pid 999999 yields `ServerState.StalePidFile`.
  - `PidFileReader_reports_not_running_when_absent()` — a missing pid file yields `ServerState.NotRunning`.
  - `AtomicFile_backs_up_once_and_writes_content()` — first `WriteText` backs up the original to `.bak`; a second write keeps the original backup and updates the file.
  - `MakeTemplate(...)` (private) — builds a template folder with `Sandbox.sbc` and optional `Sandbox_config.sbc`/extra file, returning a `WorldTemplate`.
  - `WorldCreator_copies_template_and_stamps_the_name()` — `CreateFromTemplate` copies all template files, stamps `SessionName` into `Sandbox_config.sbc`, makes the world appear in a scanned `WorldCatalog`, and leaves no dot-prefixed staging folder.
  - `WorldCreator_rejects_an_existing_world_folder()` — throws `IOException` when the target world folder already exists.
  - `WorldCreator_synthesizes_config_when_template_has_only_a_checkpoint()` — a checkpoint-only template gets a synthesized `Sandbox_config.sbc` carrying the chosen name.

## Cross-references
- **Uses:** `PasswordHasher`, `LastSessionFile`, `LaunchSpec`, `PidFileReader`, `ServerStatus`/`ServerState`, `WorldTemplate`, `WorldCreator`, `WorldCatalog`, `WorldConfigDocument`, `WorldInfo`, `InstanceBinding` (`ConfigTerminal/Model/`, `ConfigTerminal/Process/`); `AtomicFile` (`ConfigTerminal/Io/`); `System.Security.Cryptography.Rfc2898DeriveBytes`; xUnit; `System.IO`.
- **Used by:** _none within the repository_
