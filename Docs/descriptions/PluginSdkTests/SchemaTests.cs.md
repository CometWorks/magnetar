# PluginSdkTests/SchemaTests.cs

**Project:** PluginSdkTests · **Namespace:** `PluginSdk.Tests` · **Kind:** test classes · **Lines:** 525

## Summary
Specifies the schema and JSON-envelope subsystems of `PluginSdk.Config`. The file contains three test classes: `SchemaTests` drives `ConfigSchema.Build`, verifying the complete layout tree (tabs, sections, columns) extracted from class-level attributes, per-property metadata (type tokens, min/max ranges, parent container IDs), collection shapes (list element types, dict key/value types), struct member discovery (including transitive inner structs), tree-list `TreeParentField` metadata, enum member ordering and `[EnumCaption]` overrides, `[StructCaption]` validation (non-string member, missing `[StructMember]`, duplicate captions all throw `InvalidOperationException`). `JsonEnvelopeTests` drives `ConfigStorage.SaveJson`/`LoadJson`, verifying the three-part `{schema, defaults, values}` envelope shape, camelCase key names, enum-by-name encoding, sparse `defaults` vs `values` contrast, and backward-compatible flat-JSON loading. `SparseXmlTests` drives `ConfigStorage.SaveXml`/`LoadXml`, verifying that only changed properties appear in the file, that a missing property on load retains the constructor default, and that struct values at their default are omitted entirely.

## Types

### `SchemaTests` — class, public

Exercises `ConfigSchema.Build(typeof(TestConfig))`. Uses `TestConfig` as the fixture config with its full set of layout containers, scalar options, list/dict options, enum/struct options, and VRage type options.

- **Nested types (private, schema-validation stubs):**
  - `NonStringCaption` (struct) — has `[StructMember, StructCaption]` on an `int` field; used to assert that a non-string caption throws.
  - `CaptionWithoutStructMember` (struct) — `[StructCaption]` on a property that lacks `[StructMember]`; asserts the validation message.
  - `TwoCaptions` (struct) — two `[StructCaption]` members; asserts duplicate detection.
  - `NonStringCaptionConfig`, `CaptionWithoutStructMemberConfig`, `TwoCaptionsConfig` (private classes : `PluginConfig`) — minimal config wrappers exposing the invalid structs above for validation testing.

- **Methods:**
  - `Build_ExtractsLayoutContainersFromClassAttributes` — asserts 6 containers total (2 tabs + 2 sections + 2 columns), correct `Kind`, `Parent`, and `Caption` for spot-checked entries.
  - `Build_OptionsCarryParentReferences` — `Flag` → `scalars-left`, `Integer` with min/max, `LongInteger` without min/max (both `null`).
  - `Build_DescribesEachCollectionShape` — `stringList` type=`"list"`, elementType=`"string"`; `dictLongBool`/`dictIntString` key/value types.
  - `Build_StructPropertyReferencesStructDefinition` — `StructValue` type=`"struct"`, structName=`TestStruct`; struct dictionary contains `TestStruct` with 7 members (6 fields + 1 property).
  - `Build_ListOfStruct_CapturesElementStructName` — `structList` elementType=`"struct"`, elementStruct=`TestStruct`.
  - `Build_TreeListExposesTreeParentField` — `treeNodes` has treeParentField=`"ParentId"`, elementStruct=`TreeNode`.
  - `Build_NestedStruct_DescribesCollectionAndStructMembers` — `NestedStruct.Numbers` (list/int), `Map` (dict/string/double), `Inner` (struct/TestStruct).
  - `Build_NestedStruct_TransitivelyRegistersInnerStruct` — both `NestedStruct` and `TestStruct` appear in `schema.Structs` even when discovered only via nesting.
  - `Build_EnumProperty_RegistersEnumAndReferencesIt` — `Quality` type=`"enum"`, enumName=`Quality`, parent=`"scalars-right"`.
  - `Build_EnumDefinition_ListsMembersInNaturalOrderWithCaptionOverrides` — 3 members, underlying-value order (Low=0, Medium=5, High=10), caption overrides respected, fallback to member name for `High`.
  - `Build_ListOfEnum_CapturesElementEnumName` — `qualityList` elementType=`"enum"`, elementEnum=`Quality`.
  - `Build_EnumStructMember_DescribedAsEnumWithName` — `TestStruct.Quality` member type=`"enum"`, enumName=`Quality`; enum registered transitively.
  - `Build_StructCaption_EmitsCaptionMemberName` — `TreeNode.CaptionMember == "Label"`.
  - `Build_StructWithoutCaption_LeavesCaptionMemberNull` — `TestStruct` and `NestedStruct` have null `CaptionMember`.
  - `Build_StructCaption_OnNonStringMember_Throws` / `Build_StructCaption_WithoutStructMember_Throws` / `Build_StructCaption_DuplicateOnSameStruct_Throws` — negative validation paths, each asserts `InvalidOperationException` with a specific substring.

---

### `JsonEnvelopeTests` — class, public

Exercises `ConfigStorage.SaveJson` and `ConfigStorage.LoadJson`.

- **Methods:**
  - `SaveJson_ProducesEnvelopeWithSchemaDefaultsAndValues` — top-level JSON has `schema`, `defaults`, `values` properties.
  - `SaveJson_IncludesAllOptionsEvenAtDefault` — `values` section contains camelCase entries for every option declared in `TestConfig`, even at their default value (comprehensive exhaustive list checked).
  - `SaveJson_EnumValuesAreSerialisedByMemberName` — `Quality.High` → `"High"`, list of enums → array of name strings; underlying integer must not appear.
  - `SaveJson_DefaultsSectionMirrorsFreshInstance` — `defaults` reflects `new TestConfig()` while `values` reflects the modified instance.
  - `SaveJson_SchemaMatchesConfigSchemaBuild` — schema embedded in the envelope matches `ConfigSchema.Build` output (layout count, properties count, min/max spot-check, `captionMember` for `TreeNode`).
  - `LoadJson_AcceptsFlatValuesOnlyDocument` — flat `{"flag":true,"integer":7,...}` without the envelope is loaded correctly (backward-compat path).

---

### `SparseXmlTests` — class, public

Exercises `ConfigStorage.SaveXml` and `ConfigStorage.LoadXml` for sparse/default-omission behaviour.

- **Methods:**
  - `SaveXml_EmptyConfigWritesNoPropertyElements` — a default `TestConfig` produces XML with only the root element; no child property elements.
  - `SaveXml_WritesOnlyChangedProperties` — only `Integer`, `Text`, `IntList` appear; unchanged properties are absent.
  - `LoadXml_MissingPropertyKeepsConstructorDefault` — hand-crafted XML with only `Integer`; all others revert to ctor defaults.
  - `SaveXml_StructAtDefaultIsOmitted_BothFieldAndContents` — a struct with a non-default field is written; a struct at its default is omitted entirely.

## Cross-references
- **Uses:**
  - `PluginSdkTests/TestConfig.cs` — `TestConfig`, `TestStruct`, `TreeNode`, `NestedStruct`, `Quality`
  - `PluginSdk/Config/` — `ConfigSchema`, `ConfigStorage`, `PluginConfig`, `StructMember`, `StructCaption`, `StructOption`, `EnumCaption` attributes
  - `System.Text.Json` — JSON parsing in assertions
- **Used by:** _none within the repository_
