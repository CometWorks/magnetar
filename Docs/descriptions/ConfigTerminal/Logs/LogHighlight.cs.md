# ConfigTerminal/Logs/LogHighlight.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Logs` · **Kind:** static class + enum · **Lines:** 43

## Summary
Classifies a single log line for colour highlighting in the log viewer. Two markers matter to an operator scanning a log: the DS "Game ready" line (the same readiness marker `ReadinessDetector` watches for) that says a world finished loading, and any "Exception" line that flags a fault. Matching is case-sensitive substring (both markers are printed capitalised, so this avoids colouring incidental lowercase prose), and Exception wins over readiness when both appear since a fault is the more urgent thing to surface. Pure and dependency-free, so it is trivially unit-tested and reused by both the highlight painter and the `[` / `]` highlighted-line navigation in `LogViewerView`.

## Types
### LogHighlightKind — enum, internal
`None`, `Ready`, `Exception` — how a line should be highlighted, if at all.

### LogHighlight — static class, internal
- **Constants:** `ReadyMarker` (`"Game ready"`), `ExceptionMarker` (`"Exception"`) — the substrings that trigger each highlight.
- **Methods:**
  - `Classify(string line)` — returns `Exception` if the line contains the exception marker, else `Ready` if it contains the readiness marker, else `None`; a null/empty line is `None`.

## Cross-references
- **Uses:** `System` (`StringComparison.Ordinal`).
- **Used by:** [LogViewerView.cs](../Ui/LogViewerView.cs.md), [LogHighlightTests.cs](../../ConfigTerminalTests/LogHighlightTests.cs.md)
