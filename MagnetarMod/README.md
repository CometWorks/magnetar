# MagnetarMod

Small companion Space Engineers mod for Magnetar.

Server-side Magnetar plugins can call `PluginSdk.MissionScreens` to ask a
client to open `MyAPIGateway.Utilities.ShowMissionScreen`. The dedicated server
sends a custom secure message on channel `48731`; this mod receives it on the
client and opens the local UI popup.

Enable this folder as a world mod anywhere Magnetar plugins should use mission
screen popups.
