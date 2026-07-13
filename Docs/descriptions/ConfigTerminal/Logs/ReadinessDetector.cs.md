# ConfigTerminal/Logs/ReadinessDetector.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Logs` · **Kind:** static class · **Lines:** 38

## Summary
Detects that the DS has finished loading a world by scanning the game log's tail for the "Game ready" readiness marker (§2.9). It reads only the last 64 KB so it stays cheap when polled during world creation, and never throws for IO problems — a missing or locked log simply reads as "not ready yet". Note: this type is currently **unused** — a vestige of an earlier staged world-creation plan; it is retained but nothing in the codebase calls it.

## Types
### ReadinessDetector — static class, internal
Stateless tail scan for the DS readiness marker.

- **Fields:** `Marker` ("Game ready") — the line the DS prints once the session is loaded and joinable; `TailBytes` (64 KB) — how much of the file's tail to scan.
- **Methods:**
  - `IsReady(string path)` — returns false for a null/empty path; otherwise opens the file shared read/write, seeks to `max(0, length - TailBytes)`, reads the remainder as UTF-8, and returns whether it contains `Marker` (case-insensitive). Swallows `IOException`/`UnauthorizedAccessException`, returning false.

## Cross-references
- **Uses:** `System.IO` (`FileStream`, `StreamReader`, `SeekOrigin`), `System.Text.Encoding`.
- **Used by:** [LiveEndToEndTests.cs](../../ConfigTerminalTests/LiveEndToEndTests.cs.md)
