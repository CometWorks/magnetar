# PluginSdk/MissionScreenContent.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk` · **Kind:** readonly struct · **Lines:** 35

## Summary

Immutable value type carrying the text payload that the Magnetar client mod renders through Space Engineers' mission-screen popup. It is the argument type for the `MissionScreens` facade's `ShowTo*` methods and maps one-to-one onto the fields of SE's mission-screen UI (title, current-objective prefix and text, description body, and OK-button caption). Being a `readonly struct` keeps the payload cheap to pass and free of aliasing concerns when handed to the host sender for serialization.

## Types

### `MissionScreenContent` — readonly struct, public

Holds the five text fields shown on a mission-screen popup and exposes a guard that detects whether any meaningful content is present, used by `MissionScreens` to skip sending empty payloads.

- **Properties:**
  - `ScreenTitle` (`string`, get) — popup title
  - `CurrentObjectivePrefix` (`string`, get) — label preceding the current objective
  - `CurrentObjective` (`string`, get) — current-objective text
  - `ScreenDescription` (`string`, get) — main description/body text
  - `OkButtonCaption` (`string`, get) — caption for the dismiss button; may be null for the default caption
  - `HasContent` (`bool`, get) — `true` when at least one of `ScreenTitle`, `CurrentObjectivePrefix`, `CurrentObjective`, or `ScreenDescription` is non-empty (`OkButtonCaption` alone does not count as content)
- **Methods:**
  - `MissionScreenContent(string screenTitle, string currentObjectivePrefix, string currentObjective, string screenDescription, string okButtonCaption = null)` (constructor) — assigns all five fields; the OK-button caption is optional

## Cross-references

- **Uses:** `System` (BCL `string`)
- **Used by:** [MissionScreenSender.cs](../Legacy/Integration/MissionScreenSender.cs.md), [MissionScreens.cs](MissionScreens.cs.md)
