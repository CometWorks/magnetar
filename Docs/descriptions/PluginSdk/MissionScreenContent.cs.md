# PluginSdk/MissionScreenContent.cs

**Project:** PluginSdk · **Namespace:** `PluginSdk` · **Kind:** readonly struct · **Lines:** 35

## Summary
`MissionScreenContent` is the immutable payload a plugin passes to `MissionScreens` when it wants Magnetar to show the Space Engineers mission-screen popup on clients.

## Types
### MissionScreenContent — readonly struct, public
Carries the text fields accepted by `MyAPIGateway.Utilities.ShowMissionScreen`.

- **Properties:** `ScreenTitle`, `CurrentObjectivePrefix`, `CurrentObjective`, `ScreenDescription`, `OkButtonCaption`.
- **Property:** `HasContent` — true when any visible title/objective/description field is non-empty.
- **Constructor:** accepts all mission-screen text fields; `okButtonCaption` is optional.

## Cross-references
- **Uses:** BCL strings only.
- **Used by:** [MissionScreenSender.cs](../Legacy/Integration/MissionScreenSender.cs.md), [MissionScreens.cs](MissionScreens.cs.md)
