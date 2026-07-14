# ConfigTerminal/Model/Json/MiniJson.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model.Json` · **Kind:** static class, sealed class · **Lines:** 223

## Summary
A tiny, self-contained JSON reader — enough to parse the Steam Web API responses the Workshop resolver consumes, with zero third-party dependencies. It produces a lenient `JsonValue` tree; malformed input throws `FormatException`.

## Types
### MiniJson — static class, internal
- **Methods:** `Parse(string text)` — parses one value and returns the `JsonValue` root.
### MiniJson.Parser — sealed class, private (nested)
Recursive-descent parser over the input string.
- **Methods:** `SkipWhitespace()`, `ParseValue()` (dispatches on the first char), `ParseObject()`, `ParseArray()`, `ParseString()` (with `\uXXXX` and escape handling), `ParseNumber()` (invariant-culture double), `Expect(literal)`.
### JsonValue — sealed class, internal
A parsed JSON value with lenient, null-safe accessors.
- **Nested:** `Kind` enum (`Null`/`Bool`/`Number`/`String`/`Array`/`Object`).
- **Factory methods (static):** `Null()`, `Of(bool)`, `Of(double)`, `Of(string)`, `Of(List<JsonValue>)`, `Of(Dictionary<string,JsonValue>)`.
- **Properties:** `IsNull`, `IsArray`; indexer `this[string key]` (null value when absent/not an object); `Items` (array items, empty when not an array).
- **Methods:** `AsString()`, `AsLong(fallback)`, `AsInt(fallback)` — lenient conversions across kinds.

## Cross-references
- **Uses:** `System.Globalization`, `System.Text`; consumed by `WorkshopResolver` (parent module).
- **Used by:** [WorkshopResolver.cs](../WorkshopResolver.cs.md)
