# ConfigTerminalTests/LogHighlightTests.cs

**Project:** ConfigTerminalTests · **Namespace:** `Magnetar.ConfigTerminal.Tests` · **Kind:** class · **Lines:** 34

## Summary
Unit tests for `LogHighlight.Classify`, the log-viewer line classifier. Covers the two markers (a timestamped "Game ready..." line and various "Exception" lines), the None case for plain lines and null/empty input, the Exception-wins-over-Ready precedence when both appear, and the case-sensitivity that keeps lowercase prose ("no exception to the rule") from tripping the markers. Expected values are passed as the enum name string so the public test methods do not expose the internal `LogHighlightKind`.

## Types
### LogHighlightTests — class, public
- **Methods:**
  - `Classify_matches_markers(string line, string expected)` — `[Theory]` over ready/exception/none/null inputs, comparing `LogHighlight.Classify(line).ToString()` to the expected enum name.
  - `Exception_wins_over_ready_when_both_present()` — a line containing both markers classifies as `Exception`.
  - `Matching_is_case_sensitive_to_avoid_prose_false_positives()` — lowercase "exception"/"game ready" classify as `None`.

## Cross-references
- **Uses:** `LogHighlight` (`ConfigTerminal/Logs/`); xUnit (`Theory`, `InlineData`, `Fact`).
- **Used by:** _none within the repository_
