using System;
using System.Collections.Generic;
using System.Reflection;
using PluginSdk.Commands;
using VRage.Game.ModAPI;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// <see cref="ServerCommands.Execute"/> runs a command line through the
    /// host's registered commands for a plugin's own console.
    /// </summary>
    [Collection("ServerCommands")]
    public class ServerCommandsExecuteTests
    {
        [CommandRoot("run", "Run Plugin")]
        public sealed class RunCommands : CommandModule
        {
            [Command("", "default")]
            public string Root(int count = 1) =>
                $"ran {count} as {Context.Caller.Name}, console={Context.Caller.IsConsole}";
        }

        private sealed class Responder : ICommandResponder
        {
            public readonly List<string> Texts = new List<string>();

            public void Send(in CommandReply reply, in CommandCaller caller) =>
                Texts.Add(reply.Text);
        }

        // What a host does: one object that registers and runs commands.
        private sealed class Host : ICommandRegistrar, ICommandExecutor
        {
            private readonly CommandRegistry registry = new CommandRegistry();
            private readonly CommandDispatcher dispatcher;

            public Host()
            {
                dispatcher = new CommandDispatcher(registry);
            }

            public void Register(Assembly assembly) => registry.RegisterAssembly(assembly);

            public void Register(Assembly assembly, params Type[] moduleTypes)
            {
                foreach (Type type in moduleTypes)
                    registry.RegisterModule(type, assembly.GetName().Name);
            }

            public bool Execute(
                string commandLine,
                in CommandCaller caller,
                ICommandResponder responder
            ) => dispatcher.Handle(commandLine, caller, responder);
        }

        private sealed class RegistrarOnly : ICommandRegistrar
        {
            public void Register(Assembly assembly) { }

            public void Register(Assembly assembly, params Type[] moduleTypes) { }
        }

        private static readonly CommandCaller Console = new CommandCaller(
            0UL,
            0L,
            "Quasar",
            MyPromoteLevel.Owner,
            isConsole: true
        );

        [Fact]
        public void Execute_RunsTheCommandForTheGivenCaller()
        {
            ICommandRegistrar saved = ServerCommands.Registrar;
            try
            {
                ServerCommands.Registrar = new Host();
                ServerCommands.Register(typeof(RunCommands).Assembly, typeof(RunCommands));
                var responder = new Responder();

                Assert.True(ServerCommands.Execute("!run 3", Console, responder));
                Assert.Equal("ran 3 as Quasar, console=True", Assert.Single(responder.Texts));
                Assert.False(ServerCommands.Execute("!nosuchroot", Console, responder));
                Assert.False(ServerCommands.Execute("plain chat", Console, responder));
            }
            finally
            {
                ServerCommands.Registrar = saved;
            }
        }

        [Fact]
        public void Execute_HostThatCannotRunCommands_Throws()
        {
            ICommandRegistrar saved = ServerCommands.Registrar;
            try
            {
                ServerCommands.Registrar = new RegistrarOnly();
                Assert.Throws<InvalidOperationException>(() =>
                    ServerCommands.Execute("!run", Console, new Responder())
                );
            }
            finally
            {
                ServerCommands.Registrar = saved;
            }
        }
    }
}
