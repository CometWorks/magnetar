export const meta = {
  name: 'magnetar-describe',
  description: 'Generate per-file C# description docs for each Magnetar module and return module summaries',
  phases: [{ title: 'Describe', detail: 'one agent per module writes per-file description docs' }],
}

const ROOT = '/home/viktor/dev/se1/Magnetar'

const modules = {"Compiler":{"project":"Compiler","files":["Compiler/LogFile.cs","Compiler/PublicizedAssemblies.cs","Compiler/Publicizer.cs","Compiler/RoslynCompiler.cs","Compiler/RoslynReferences.cs"],"model":"opus"},"Legacy.Commands":{"project":"Legacy","files":["Legacy/Commands/CommandService.cs","Legacy/Commands/MagnetarCommands.cs","Legacy/Commands/ServerCommandResponder.cs"],"model":"sonnet"},"Legacy.Integration":{"project":"Legacy","files":["Legacy/Compiler/Interim.cs","Legacy/Compiler/Legacy.cs","Legacy/Compiler/References.cs","Legacy/Extensions/ModPlugin.cs","Legacy/Paths/PathResolverBinder.cs","Legacy/Paths/ReflectionPathResolver.cs"],"model":"sonnet"},"Legacy.Launcher":{"project":"Legacy","files":["Legacy/Launcher/Folder.cs","Legacy/Launcher/Game.cs","Legacy/Launcher/ServerControl.cs","Legacy/Program.cs"],"model":"opus"},"Legacy.Loader":{"project":"Legacy","files":["Legacy/Loader/LoaderTools.cs","Legacy/Loader/NativeLibraryPreloader.cs","Legacy/Loader/PluginInstance.cs","Legacy/Loader/PluginLoader.cs","Legacy/Loader/SteamMods.cs"],"model":"opus"},"Legacy.Patch":{"project":"Legacy","files":["Legacy/Patch/Patch_Compile.cs","Legacy/Patch/Patch_ComponentRegistered.cs","Legacy/Patch/Patch_DedicatedServerRun.cs","Legacy/Patch/Patch_ExitThreadSafe.cs","Legacy/Patch/Patch_LoadScripts.cs","Legacy/Patch/Patch_MyDefinitionErrors.cs","Legacy/Patch/Patch_MyDefinitionManager.cs","Legacy/Patch/Patch_MyScriptManager.cs","Legacy/Patch/Patch_MySessionLoader.cs","Legacy/Patch/Patch_PrepareCrashReport.cs","Legacy/Patch/Patch_ServerChat.cs"],"model":"sonnet"},"PluginSdk.Commands":{"project":"PluginSdk","files":["PluginSdk/Commands/ArgumentBinder.cs","PluginSdk/Commands/CommandAttribute.cs","PluginSdk/Commands/CommandCaller.cs","PluginSdk/Commands/CommandContext.cs","PluginSdk/Commands/CommandDispatcher.cs","PluginSdk/Commands/CommandLine.cs","PluginSdk/Commands/CommandModule.cs","PluginSdk/Commands/CommandRegistrationException.cs","PluginSdk/Commands/CommandRegistry.cs","PluginSdk/Commands/CommandReply.cs","PluginSdk/Commands/CommandRoot.cs","PluginSdk/Commands/CommandRootAttribute.cs","PluginSdk/Commands/ICommandRegistrar.cs","PluginSdk/Commands/ICommandResponder.cs","PluginSdk/Commands/PermissionAttribute.cs","PluginSdk/Commands/RegisteredCommand.cs","PluginSdk/Commands/ServerCommands.cs"],"model":"sonnet"},"PluginSdk.Config":{"project":"PluginSdk","files":["PluginSdk/Config/ConfigAttributes.cs","PluginSdk/Config/ConfigSchema.cs","PluginSdk/Config/ConfigStorage.cs","PluginSdk/Config/PluginConfig.cs","PluginSdk/Config/TypeSerialization.cs"],"model":"opus"},"PluginSdk.Logging":{"project":"PluginSdk","files":["PluginSdk/Logging/ILogSink.cs","PluginSdk/Logging/LogEntry.cs","PluginSdk/Logging/LogEnvironment.cs","PluginSdk/Logging/LogJson.cs","PluginSdk/Logging/LogLevel.cs","PluginSdk/Logging/Logger.cs","PluginSdk/Logging/MagnetarLogSink.cs","PluginSdk/Logging/QuasarLogSink.cs"],"model":"sonnet"},"PluginSdk.Runtime":{"project":"PluginSdk","files":["PluginSdk/Paths/IPathResolver.cs","PluginSdk/Paths/PathResolver.cs","PluginSdk/Paths/ShimPathResolver.cs","PluginSdk/ServerControl.cs","PluginSdk/Tools/SerializableDictionary.cs"],"model":"sonnet"},"PluginSdkTests":{"project":"PluginSdkTests","files":["PluginSdkTests/ChangeNotificationTests.cs","PluginSdkTests/CommandTests.cs","PluginSdkTests/LoggingTests.cs","PluginSdkTests/PathResolverTests.cs","PluginSdkTests/SchemaTests.cs","PluginSdkTests/SerializationTests.cs","PluginSdkTests/ServerControlTests.cs","PluginSdkTests/TestConfig.cs"],"model":"sonnet"},"Shared.Config":{"project":"Shared","files":["Shared/Config/ConfigManager.cs","Shared/Config/CoreConfig.cs","Shared/Config/GitHubPluginConfig.cs","Shared/Config/LocalFolderConfig.cs","Shared/Config/PluginDataConfig.cs","Shared/Config/ProfilesConfig.cs","Shared/Config/Sources/LocalHubConfig.cs","Shared/Config/Sources/LocalPluginConfig.cs","Shared/Config/Sources/ModConfig.cs","Shared/Config/Sources/RemoteHubConfig.cs","Shared/Config/Sources/RemotePluginConfig.cs","Shared/Config/SourcesConfig.cs"],"model":"sonnet"},"Shared.Core":{"project":"Shared","files":["Shared/AssemblyResolver.cs","Shared/Flags.cs","Shared/Launcher.cs","Shared/Loader.cs","Shared/LogFile.cs","Shared/PluginList.cs","Shared/PluginProgress.cs","Shared/Preloader.cs","Shared/Steam.cs","Shared/Tools.cs","Shared/Updater.cs"],"model":"opus"},"Shared.Data":{"project":"Shared","files":["Shared/Data/GitHubPlugin.AssetFile.cs","Shared/Data/GitHubPlugin.CacheManifest.cs","Shared/Data/GitHubPlugin.cs","Shared/Data/LocalFolderPlugin.cs","Shared/Data/LocalPlugin.cs","Shared/Data/ModPlugin.cs","Shared/Data/ObsoletePlugin.cs","Shared/Data/PluginData.cs","Shared/Data/PluginStatus.cs","Shared/Data/Profile.cs"],"model":"opus"},"Shared.Network":{"project":"Shared","files":["Shared/Network/GitHub.cs","Shared/Network/NuGetClient.cs","Shared/Network/NuGetLogger.cs","Shared/Network/NuGetPackage.cs","Shared/Network/NuGetPackageId.cs","Shared/Network/NuGetPackageList.cs","Shared/Network/SimpleHttpClient.cs"],"model":"sonnet"},"Shared.Votes":{"project":"Shared","files":["Shared/Votes/Model/ConsentRequest.cs","Shared/Votes/Model/PluginVote.cs","Shared/Votes/Model/PluginVotes.cs","Shared/Votes/Model/TrackRequest.cs","Shared/Votes/Model/VoteRequest.cs","Shared/Votes/VotesClient.cs"],"model":"sonnet"}}

const SUMMARY_SCHEMA = {
  type: 'object',
  additionalProperties: false,
  required: ['module', 'project', 'purpose', 'role', 'key_types', 'public_api', 'depends_on', 'files_documented'],
  properties: {
    module: { type: 'string' },
    project: { type: 'string' },
    purpose: { type: 'string', description: '1-3 sentence what/why of this module' },
    role: { type: 'string', description: 'How it fits in the overall Magnetar launcher/SDK architecture' },
    key_types: {
      type: 'array',
      items: {
        type: 'object',
        additionalProperties: false,
        required: ['name', 'kind', 'file', 'summary'],
        properties: {
          name: { type: 'string' },
          kind: { type: 'string', description: 'class | struct | interface | enum | static class | record' },
          file: { type: 'string', description: 'repo-relative path' },
          summary: { type: 'string', description: 'one line' },
        },
      },
    },
    public_api: { type: 'array', items: { type: 'string' }, description: 'Notable public entry points other modules call' },
    depends_on: { type: 'array', items: { type: 'string' }, description: 'Other modules or external systems this module relies on' },
    files_documented: { type: 'array', items: { type: 'string' } },
  },
}

const entries = Object.entries(modules)

const summaries = await parallel(entries.map(([name, info]) => () => {
  const fileList = info.files.map(f => `  - ${f}`).join('\n')
  const prompt = `You are documenting the **${name}** module of Magnetar, a plugin/mod loader for the Space Engineers (SE1) Dedicated Server. Magnetar is a hard fork of Pulsar (namespaces are \`Pulsar.*\`), adapted to run the headless DS on both Windows (.NET Framework 4.8 = "Legacy") and Linux/.NET 10 (= "Interim"). Project of this module: ${info.project}.

Repo root: ${ROOT}

## Source files to document (read EVERY one in full)
${fileList}

## Your task
For EACH source file, write a Markdown description file. The description path mirrors the source path under \`${ROOT}/Docs/descriptions/\`, with \`.md\` appended. Example: source \`Shared/PluginList.cs\` -> write \`${ROOT}/Docs/descriptions/Shared/PluginList.cs.md\`. The parent directories already exist.

Read the actual code with the Read tool before writing — never guess. Understand control flow, the SE/Harmony/Steam/Roslyn/NuGet APIs involved, and why each type exists. Be accurate and concrete; this is a developer handbook, not marketing.

## Exact description file format (keep this structure and item order for every file)
\`\`\`markdown
# <repo-relative source path>

**Project:** <project> · **Namespace:** <namespace(s)> · **Kind:** <primary type kind> · **Lines:** <n>

## Summary
<2-5 sentences: what this file provides and why it exists in Magnetar. Mention the concrete SE DS / external API it wraps or patches if relevant.>

## Types
For each type declared in the file:
### <TypeName> — <kind>, <visibility>[ : <base / interfaces>]
<one-paragraph description of the type's responsibility>
- **Fields:** <name — purpose; ...>  (omit the bullet if none)
- **Properties:** <name — purpose; ...>  (omit if none)
- **Methods:** <signature-ish name — what it does; ...>  (cover all non-trivial members; group trivial overloads)
- **Events:** <name — when raised; ...>  (omit if none)

## Cross-references
- **Uses:** <other source files / modules / external systems this file depends on — repo-relative paths where internal>
- **Used by:** _to be filled by propagation_
\`\`\`

Rules:
- Document ALL public/internal types and their members. For tier-3 tiny DTO/enum/interface files, a brief Types section is fine but still list members.
- Keep prose tight. No filler. Use backticks for type/member/identifier names.
- If a file is a Harmony patch, state the exact target type+method it patches and the patch kind (Prefix/Postfix/Transpiler) and why.
- Do not invent members. If unsure, read again.

After writing every description file, return the structured module summary (the StructuredOutput tool). \`files_documented\` must list every repo-relative source path you wrote a description for. \`depends_on\` should name other Magnetar modules (Shared.Core, Shared.Data, Shared.Config, Shared.Network, Shared.Votes, Compiler, PluginSdk.Commands, PluginSdk.Config, PluginSdk.Logging, PluginSdk.Runtime, Legacy.Launcher, Legacy.Loader, Legacy.Patch, Legacy.Commands, Legacy.Integration) and/or external systems (Steam, GitHub, NuGet, Harmony, SE DS assemblies).`

  return agent(prompt, {
    label: `describe:${name}`,
    phase: 'Describe',
    model: info.model,
    schema: SUMMARY_SCHEMA,
  }).then(s => s || { module: name, project: info.project, purpose: '', role: '', key_types: [], public_api: [], depends_on: [], files_documented: [] })
}))

return { summaries: summaries.filter(Boolean) }
