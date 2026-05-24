# Magnetar
A plugin loader for the Space Engineers Dedicated Server.<br>
This is a hard fork of [Pulsar](https://github.com/SpaceGT/Pulsar), adapted to load the Dedicated Server instead of the game client.<br>

## Installation
Magnetar is portable: simply download the [latest release](https://github.com/viktor-ferenczi/Magnetar/releases/latest) into a folder of choice.<br>
This folder **must not** contain important data; It **will be cleaned** during a Magnetar update! <br>
If you are building from source, the deploy script will copy all files to their required location.<br>

## Executables
`Legacy` runs the [Space Engineers 1](https://steampowered.com/app/244850) Dedicated Server on [.NET Framework 4.8](https://dotnet.microsoft.com/en-us/download/dotnet-framework)<br>
`Interim` runs the [Space Engineers 1](https://steampowered.com/app/244850) Dedicated Server on [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
(via [se-dotnet-compat](https://github.com/viktor-ferenczi/se-dotnet-compat))<br>

## Usage
Run the `Legacy` or `Interim` executable from your Dedicated Server installation in place of `SpaceEngineersDedicated.exe`.<br>
Magnetar loads the server with all enabled plugins, then hands off to the normal startup.<br>

## Plugins
Plugins can be registered at [PluginHub-DS](https://github.com/viktor-ferenczi/PluginHub-DS/).<br>
Further sources may be added but make sure you fully understand the risks.<br>

## Contact
We have an active [Discord](https://discord.gg/z8ZczP2YZY) for updates and developer information.<br>
GitHub contributions and bug reports are also welcomed!<br>
We prefer Discord over GitHub for support-related queries.<br>
