# Shared/Network/NuGetPackageId.cs

**Project:** Shared · **Namespace:** `Pulsar.Shared.Network` · **Kind:** class · **Lines:** 47

## Summary
`NuGetPackageId` is a serialisable DTO that identifies a single NuGet package by name and version string. It is dual-serialised: ProtoBuf attributes are used for binary inter-process communication (plugin metadata transferred between Loader and Compiler), while XML attributes allow it to be parsed from MSBuild-style `<PackageReference Include="..." Version="...">` elements or from element-form `<Name>`/`<Version>` markup. The `TryGetIdentity` helper bridges into the NuGet SDK's `PackageIdentity` type used by `NuGetClient`.

## Types

### `NuGetPackageId` — class, public

Simple two-property record with two alternate XML serialisation shapes for the same logical fields, plus a conversion helper.

- **Properties:**
  - `Name` — `[ProtoMember(1)]`, `[XmlElement]`; the NuGet package ID (element form, e.g., `<Name>Newtonsoft.Json</Name>`)
  - `NameAttribute` — `[ProtoIgnore]`, `[XmlAttribute("Include")]`; attribute alias for `Name` in MSBuild `<PackageReference Include="...">` syntax; getter/setter proxy to `Name`
  - `Version` — `[ProtoMember(2)]`, `[XmlElement]`; the package version string (element form)
  - `VersionAttribute` — `[ProtoIgnore]`, `[XmlAttribute("Version")]`; attribute alias for `Version` in `<PackageReference Version="...">` syntax; getter/setter proxy to `Version`

- **Methods:**
  - `TryGetIdentity(out PackageIdentity id)` — returns `false` if `Name` or `Version` is null/whitespace or if `NuGetVersion.TryParse` fails; on success sets `id` to `new PackageIdentity(Name, version)` and returns `true`

## Cross-references
- **Uses:**
  - External: `NuGet.Packaging.Core.PackageIdentity`
  - External: `NuGet.Versioning.NuGetVersion`
  - External: `ProtoBuf` (`ProtoContract`, `ProtoMember`, `ProtoIgnore`)
  - External: `System.Xml.Serialization` (`XmlElement`, `XmlAttribute`)
- **Used by:** [NuGetClient.cs](NuGetClient.cs.md), [NuGetPackageList.cs](NuGetPackageList.cs.md)
