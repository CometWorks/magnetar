# ConfigTerminal/Io/XmlOut.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Io` · **Kind:** static class · **Lines:** 43

## Summary
Shared XML output settings matching what the DS and Quasar write — UTF-8 without BOM, indented, LF (`\n`) newlines, XML declaration present — plus a helper that serializes an `XDocument` to a string with those settings. Keeping every DS file on identical settings keeps diffs clean across platforms and tools.

## Types
### XmlOut — static class, internal
- **Fields:**
  - `Utf8NoBom` (static `UTF8Encoding`) — UTF-8 without BOM.
- **Methods:**
  - `Settings()` — returns a fresh `XmlWriterSettings` with `Indent = true`, `Encoding = Utf8NoBom`, `OmitXmlDeclaration = false`, `NewLineChars = "\n"`, `NewLineHandling = Replace`.
  - `ToXmlString(XDocument)` — serializes the document to a string via `XmlWriter.Create` over a `Utf8StringWriter` using `Settings()`; flushes and returns the text.

### Utf8StringWriter — sealed class, private (nested)
A `StringWriter` whose `Encoding` override reports UTF-8 so the emitted XML declaration reads `encoding="utf-8"` (a plain `StringWriter` would report UTF-16).

## Cross-references
- **Uses:** `System.Xml` (`XmlWriter`, `XmlWriterSettings`, `NewLineHandling`); `System.Xml.Linq.XDocument`; `System.IO.StringWriter`; `System.Text.UTF8Encoding`.
- **Used by:** [ConfigDocumentBase.cs](../Model/ConfigDocumentBase.cs.md), [LastSessionFile.cs](../Model/LastSessionFile.cs.md), [PluginProfileDocument.cs](../Model/PluginProfileDocument.cs.md), [PluginSourcesDocument.cs](../Model/PluginSourcesDocument.cs.md), [ToolSettings.cs](../State/ToolSettings.cs.md)
