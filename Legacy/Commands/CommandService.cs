using System;
using System.Reflection;
using PluginSdk.Commands;
using Pulsar.Shared;
using Sandbox.Game.World;
using VRage.Game.ModAPI;

namespace Magnetar.Legacy.Commands;

/// <summary>
/// Host-side owner of the chat-command pipeline: it holds the
/// <see cref="CommandRegistry"/> that plugins fill via the
/// <see cref="ServerCommands"/> facade, and routes incoming chat to the
/// <see cref="CommandDispatcher"/>. A single instance is installed as the
/// host's <see cref="ICommandRegistrar"/>; ownership of each command is taken
/// from the registering plugin's assembly.
/// </summary>
public sealed class CommandService : ICommandRegistrar
{
    private readonly CommandRegistry registry = new();
    private readonly CommandDispatcher dispatcher;

    public CommandService()
    {
        dispatcher = new CommandDispatcher(registry, OnHandlerError);
    }

    /// <summary>Scans <paramref name="assembly"/> for command modules,
    /// attributing them to that assembly. Conflicting commands are
    /// overwritten (last registration wins).</summary>
    public void Register(Assembly assembly)
    {
        string ownerId = assembly.GetName().Name;
        try
        {
            int count = registry.RegisterAssembly(assembly);
            if (count > 0)
                LogFile.WriteLine($"Registered {count} command module(s) from {ownerId}");
        }
        catch (CommandRegistrationException e)
        {
            LogFile.Error($"Failed to register commands from {ownerId}: {e.Message}");
        }
        catch (Exception e)
        {
            LogFile.Error($"Unexpected error registering commands from {ownerId}: {e}");
        }
    }

    /// <summary>Registers the explicitly named module types, attributing them
    /// to <paramref name="assembly"/>. Conflicting commands are overwritten
    /// (last registration wins).</summary>
    public void Register(Assembly assembly, params Type[] moduleTypes)
    {
        if (moduleTypes == null)
            return;

        string ownerId = assembly.GetName().Name;
        int count = 0;
        foreach (Type moduleType in moduleTypes)
        {
            try
            {
                registry.RegisterModule(moduleType, ownerId);
                count++;
            }
            catch (CommandRegistrationException e)
            {
                LogFile.Error($"Failed to register command module {moduleType} from {ownerId}: {e.Message}");
            }
            catch (Exception e)
            {
                LogFile.Error($"Unexpected error registering command module {moduleType} from {ownerId}: {e}");
            }
        }

        if (count > 0)
            LogFile.WriteLine($"Registered {count} command module(s) from {ownerId}");
    }

    /// <summary>
    /// Handles a chat line from <paramref name="steamId"/>. Returns <c>true</c>
    /// when the line was a command for a registered root and should be
    /// suppressed from normal chat.
    /// </summary>
    public bool HandleChat(ulong steamId, string text)
    {
        if (MySession.Static == null || string.IsNullOrEmpty(text))
            return false;

        try
        {
            CommandCaller caller = BuildCaller(steamId);
            return dispatcher.Handle(text, caller, ServerCommandResponder.Instance);
        }
        catch (Exception e)
        {
            LogFile.Error($"Error handling chat command '{text}': {e}");
            return false;
        }
    }

    private static CommandCaller BuildCaller(ulong steamId)
    {
        long identityId = MySession.Static.Players.TryGetIdentityId(steamId);
        MyPromoteLevel level = MySession.Static.GetUserPromoteLevel(steamId);
        MyIdentity identity = MySession.Static.Players.TryGetIdentity(identityId);
        string name = identity?.DisplayName ?? steamId.ToString();
        return new CommandCaller(steamId, identityId, name, level);
    }

    private static void OnHandlerError(string message, Exception ex)
        => LogFile.Error($"{message}: {ex}");
}
