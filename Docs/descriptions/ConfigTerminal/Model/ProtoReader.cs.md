# ConfigTerminal/Model/ProtoReader.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** sealed class · **Lines:** 117

## Summary
A tiny, forward-only reader for the Protocol Buffers wire format — just enough to walk Magnetar's protobuf-net hub-catalog cache (`Sources/Hubs/*.bin`, `Sources/Plugins/*.bin`) by field number, without referencing `Shared`/protobuf-net or loading any Magnetar type. Unknown fields are skipped so the reader degrades gracefully as the catalog schema grows.

## Types
### ProtoReader — sealed class, internal
- **Consts:** wire types `WireVarint` (0), `WireFixed64` (1), `WireLengthDelimited` (2), `WireFixed32` (5).
- **Fields:** `buffer`, `pos`, `end`.
- **Constructors:** `ProtoReader(byte[])` and `ProtoReader(byte[], offset, length)` (null-safe).
- **Properties:** `AtEnd`.
- **Methods:**
  - `ReadTag(out int fieldNumber, out int wireType)` — reads the next tag; false at end of message.
  - `ReadVarint()` — decodes a base-128 varint (bounded to 64 bits).
  - `ReadString()` — length-delimited UTF-8; safely truncates on an out-of-range length.
  - `ReadMessage()` — returns a sub-reader over the next length-delimited field's bytes.
  - `Skip(int wireType)` — skips a field of the given wire type; bails to end on an unknown wire type to avoid a loop.

## Cross-references
- **Uses:** `System.Text.Encoding`; consumed by `HubCatalog` (this module).
- **Used by:** [HubCatalog.cs](HubCatalog.cs.md)
