using System;
using System.Collections.Generic;
using PluginSdk.Commands;
using VRage.Game.ModAPI;
using Xunit;

namespace PluginSdk.Tests
{
    public class CommandAliasTests
    {
        [CommandRoot("ess")]
        public sealed class Commands : CommandModule
        {
            [Command("grids list")]
            [Permission(MyPromoteLevel.None)]
            public string List(string name = "all") => name;

            [Command("secure")]
            public string Secure() => $"{Context.Caller.SteamId}/{Context.Caller.IdentityId}/{Context.Caller.Name}/{Context.Caller.IsConsole}";
        }

        private sealed class Responder : ICommandResponder
        {
            internal readonly List<CommandReply> Replies = new List<CommandReply>();
            internal readonly List<CommandCaller> Callers = new List<CommandCaller>();
            public void Send(in CommandReply reply, in CommandCaller caller)
            { Replies.Add(reply); Callers.Add(caller); }
        }

        private static CommandDispatcher Dispatcher(params (string Source, string Target)[] pairs)
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(Commands), "essentials");
            var dispatcher = new CommandDispatcher(registry);
            var aliases = new List<KeyValuePair<string, string>>();
            foreach (var pair in pairs) aliases.Add(new KeyValuePair<string, string>(pair.Source, pair.Target));
            dispatcher.SetAliases(aliases);
            return dispatcher;
        }

        private static readonly CommandCaller Player = new CommandCaller(123, 456, "Player", MyPromoteLevel.None);

        [Fact]
        public void LongestWholeTokenMatchPreservesQuotedAndEscapedArguments()
        {
            var dispatcher = Dispatcher(("!grids", "!ess secure"), ("!grids list", "!ess grids list"));
            foreach (string message in new[] { "!GRIDS\tLIST \"Big Ship\"", "!grids list Big\\ Ship" })
            {
                var responder = new Responder();
                Assert.True(dispatcher.Handle(message, Player, responder));
                Assert.Equal("Big Ship", Assert.Single(responder.Replies).Text);
            }
            Assert.False(dispatcher.Handle("!gridslist", Player, new Responder()));
            Assert.False(dispatcher.Handle("ordinary chat", Player, new Responder()));
        }

        [Fact]
        public void TargetPermissionsAndCallerIdentityAreUnchanged()
        {
            var dispatcher = Dispatcher(("!secure", "!ess secure"));
            var denied = new Responder();
            Assert.True(dispatcher.Handle("!secure", Player, denied));
            Assert.Contains("permission", Assert.Single(denied.Replies).Text);
            Assert.False(denied.Replies[0].Broadcast);
            var admin = new CommandCaller(987, 654, "Admin", MyPromoteLevel.Admin);
            var allowed = new Responder();
            dispatcher.Handle("!secure", admin, allowed);
            Assert.Equal("987/654/Admin/False", Assert.Single(allowed.Replies).Text);
            Assert.Equal(987UL, Assert.Single(allowed.Callers).SteamId);
            Assert.Equal(654L, allowed.Callers[0].IdentityId);
            Assert.False(allowed.Callers[0].IsConsole);
        }

        [Fact]
        public void AliasesOverrideRegisteredCommandsAndClearRestoresThem()
        {
            var dispatcher = Dispatcher(("!ess grids list", "!ess secure"));
            var responder = new Responder();
            dispatcher.Handle("!ess grids list", Player, responder);
            Assert.Contains("permission", Assert.Single(responder.Replies).Text);
            dispatcher.SetAliases(Array.Empty<KeyValuePair<string, string>>());
            responder = new Responder();
            dispatcher.Handle("!ess grids list", Player, responder);
            Assert.Equal("all", Assert.Single(responder.Replies).Text);
        }

        [Fact]
        public void CyclesAndMissingTargetsAreConsumedWithoutRecursionOrBroadcast()
        {
            var dispatcher = Dispatcher(("!a", "!b"), ("!b", "!a"));
            var responder = new Responder();
            Assert.True(dispatcher.Handle("!a secret arguments", Player, responder));
            Assert.Contains("unavailable", Assert.Single(responder.Replies).Text);
            Assert.False(responder.Replies[0].Broadcast);
        }

        [Theory]
        [InlineData("!help", "!ess grids list")]
        [InlineData("!help anything", "!ess")]
        [InlineData("!a", "!A")]
        [InlineData("a", "!ess")]
        [InlineData("!a", "!ess \"fixed argument\"")]
        [InlineData("!a\n", "!ess")]
        public void RejectsInvalidPaths(string source, string target) =>
            Assert.Throws<ArgumentException>(() => Dispatcher((source, target)));

        [Fact]
        public void SnapshotAndPathLimitsRejectOversizedUpdates()
        {
            var dispatcher = Dispatcher();
            var aliases = new List<KeyValuePair<string, string>>();
            for (int i = 0; i < 64; i++)
                aliases.Add(new KeyValuePair<string, string>("!a" + i, "!ess grids list"));
            dispatcher.SetAliases(aliases);
            aliases.Add(new KeyValuePair<string, string>("!extra", "!ess"));
            Assert.Throws<ArgumentException>(() => dispatcher.SetAliases(aliases));
            Assert.Throws<ArgumentException>(() => Dispatcher(("!" + new string('a', 512), "!ess")));
        }

        [Fact]
        public void InvalidReplacementLeavesPreviousSnapshotIntact()
        {
            var dispatcher = Dispatcher(("!a", "!ess grids list"));
            Assert.Throws<ArgumentException>(() => dispatcher.SetAliases(new[]
            {
                new KeyValuePair<string, string>("!b path", "!ess"),
                new KeyValuePair<string, string>("!B\tPATH", "!ess secure")
            }));
            var responder = new Responder();
            Assert.True(dispatcher.Handle("!a", Player, responder));
            Assert.Equal("all", Assert.Single(responder.Replies).Text);
        }
    }
}
