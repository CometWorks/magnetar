# MagnetarMod

Small companion Space Engineers mod for Magnetar.

Server-side Magnetar plugins can call `PluginSdk.MissionScreens` to ask a
client to open `MyAPIGateway.Utilities.ShowMissionScreen`. The dedicated server
sends a custom secure message on channel `48731`; this mod receives it on the
client and opens the local UI popup.

Enable this folder as a world mod anywhere Magnetar plugins should use mission
screen popups.

## Development

`MagnetarMod.csproj` is an MDK2 project for IDE support, local builds, and
ModAPI analyzer coverage. It uses the same MDK2 reference/analyzer packages as
the ShipCoreFramework mod project.

The project is part of `../Magnetar.sln`, but only the `Workshop|Any CPU`
solution configuration builds it. Normal `Debug`/`Release` solution builds skip
it so the release pipeline remains focused on the launcher/plugin-loader build.
Space Engineers compiles this mod when loaded in a world.
