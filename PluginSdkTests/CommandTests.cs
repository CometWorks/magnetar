using System;
using System.Collections.Generic;
using System.Linq;
using PluginSdk.Commands;
using VRage.Game.ModAPI;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Tests for the chat-command pipeline: registration of
    /// <see cref="CommandModule"/> types under a <c>!prefix</c> root, parsing
    /// and typed binding of arguments, permission gating and visibility,
    /// the built-in overview/help, reply conventions, and error handling — all
    /// exercised against a capturing <see cref="ICommandResponder"/> with no
    /// live game session.
    /// </summary>
    public class CommandTests
    {
        private sealed class CapturingResponder : ICommandResponder
        {
            public readonly List<CommandReply> Replies = new List<CommandReply>();
            public readonly List<CommandCaller> Callers = new List<CommandCaller>();
            public void Send(in CommandReply reply, in CommandCaller caller)
            {
                Replies.Add(reply);
                Callers.Add(caller);
            }
            public IEnumerable<string> Texts => Replies.Select(r => r.Text);
            public string LastText => Replies.Count == 0 ? null : Replies[Replies.Count - 1].Text;
        }

        [CommandRoot("test", "Test Plugin", "a demo plugin")]
        public sealed class SampleCommands : CommandModule
        {
            [Command("ping", "replies pong")]
            [Permission(MyPromoteLevel.None)]
            public string Ping() => "pong";

            [Command("add", "adds two integers")]
            [Permission(MyPromoteLevel.None)]
            public string Add(int a, int b = 1) => (a + b).ToString();

            [Command("echo", "echoes the words")]
            [Permission(MyPromoteLevel.None)]
            public CommandReply Echo(params string[] words) => CommandReply.Ok(string.Join(" ", words));

            [Command("yell", "announces to all")]
            [Permission(MyPromoteLevel.None)]
            public CommandReply Yell(string message) => CommandReply.Announce(message);

            [Command("grid list", "lists grids")]
            [Permission(MyPromoteLevel.None)]
            public string GridList() => "grids";

            [Command("boom", "always throws")]
            [Permission(MyPromoteLevel.None)]
            public string Boom() => throw new InvalidOperationException("kaboom");

            // No [Permission] => defaults to Admin.
            [Command("secret", "admin only")]
            public string Secret() => "classified";

            [Command("silent", "returns nothing")]
            [Permission(MyPromoteLevel.None)]
            public void Silent() { }
        }

        private static CommandDispatcher BuildDispatcher(out CapturingResponder responder, Action<string, Exception> onError = null)
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(SampleCommands), "owner1");
            responder = new CapturingResponder();
            return new CommandDispatcher(registry, onError);
        }

        private static CommandCaller Caller(MyPromoteLevel level)
            => new CommandCaller(123UL, 42L, "Tester", level);

        [Fact]
        public void Handle_SimpleCommand_RepliesToCaller()
        {
            var d = BuildDispatcher(out var r);
            bool handled = d.Handle("!test ping", Caller(MyPromoteLevel.None), r);

            Assert.True(handled);
            Assert.Equal("pong", Assert.Single(r.Replies).Text);
        }

        [Fact]
        public void Handle_NonCommandText_IsNotHandled()
        {
            var d = BuildDispatcher(out var r);
            Assert.False(d.Handle("hello world", Caller(MyPromoteLevel.None), r));
            Assert.Empty(r.Replies);
        }

        [Fact]
        public void Handle_UnknownPrefix_IsNotHandled()
        {
            var d = BuildDispatcher(out var r);
            Assert.False(d.Handle("!other ping", Caller(MyPromoteLevel.None), r));
            Assert.Empty(r.Replies);
        }

        [Fact]
        public void Handle_BindsTypedArgsAndDefaults()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test add 5", Caller(MyPromoteLevel.None), r);
            Assert.Equal("6", r.LastText);

            r.Replies.Clear();
            d.Handle("!test add 5 7", Caller(MyPromoteLevel.None), r);
            Assert.Equal("12", r.LastText);
        }

        [Fact]
        public void Handle_MissingRequiredArg_RepliesUsage()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test add", Caller(MyPromoteLevel.None), r);

            Assert.Contains("missing", r.LastText);
            Assert.Contains("!test add <a>", r.LastText);
        }

        [Fact]
        public void Handle_BadArgType_RepliesError()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test add notanumber", Caller(MyPromoteLevel.None), r);

            Assert.Contains("not a valid integer", r.LastText);
        }

        [Fact]
        public void Handle_ParamsArray_CapturesRemaining()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test echo a b c", Caller(MyPromoteLevel.None), r);
            Assert.Equal("a b c", r.LastText);
        }

        [Fact]
        public void Handle_QuotedArgument_KeptTogether()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test echo \"hello world\" foo", Caller(MyPromoteLevel.None), r);
            Assert.Equal("hello world foo", r.LastText);
        }

        [Fact]
        public void Handle_NestedCommandPath_Resolves()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test grid list", Caller(MyPromoteLevel.None), r);
            Assert.Equal("grids", r.LastText);
        }

        [Fact]
        public void Handle_BroadcastReply_IsMarkedBroadcast()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test yell hi", Caller(MyPromoteLevel.None), r);

            var reply = Assert.Single(r.Replies);
            Assert.True(reply.Broadcast);
            Assert.Equal("hi", reply.Text);
        }

        [Fact]
        public void Handle_VoidHandler_SendsNothing()
        {
            var d = BuildDispatcher(out var r);
            bool handled = d.Handle("!test silent", Caller(MyPromoteLevel.None), r);
            Assert.True(handled);
            Assert.Empty(r.Replies);
        }

        [Fact]
        public void Handle_InsufficientPermission_IsDenied()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test secret", Caller(MyPromoteLevel.None), r);
            Assert.Contains("permission", r.LastText);
        }

        [Fact]
        public void Handle_SufficientPermission_RunsAdminCommand()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test secret", Caller(MyPromoteLevel.Admin), r);
            Assert.Equal("classified", r.LastText);
        }

        [Fact]
        public void Overview_HidesCommandsAboveCallerLevel()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test", Caller(MyPromoteLevel.None), r);

            string all = string.Join("\n", r.Texts);
            Assert.Contains("ping", all);
            Assert.Contains("grid list", all);
            Assert.DoesNotContain("secret", all);
        }

        [Fact]
        public void Overview_ShowsAdminCommandsToAdmin()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test", Caller(MyPromoteLevel.Admin), r);
            Assert.Contains("secret", string.Join("\n", r.Texts));
        }

        [Fact]
        public void Help_ForCommand_ShowsUsage()
        {
            var d = BuildDispatcher(out var r);
            d.Handle("!test help add", Caller(MyPromoteLevel.None), r);

            string all = string.Join("\n", r.Texts);
            Assert.Contains("!test add <a> [b]", all);
        }

        [Fact]
        public void Handle_UnknownSubcommand_RepliesUnknown()
        {
            var d = BuildDispatcher(out var r);
            bool handled = d.Handle("!test nope", Caller(MyPromoteLevel.None), r);
            Assert.True(handled);
            Assert.Contains("Unknown command", r.LastText);
        }

        [Fact]
        public void Handle_HandlerException_ReportsAndReplies()
        {
            Exception captured = null;
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(SampleCommands), "owner1");
            var r = new CapturingResponder();
            var d = new CommandDispatcher(registry, (msg, ex) => captured = ex);

            d.Handle("!test boom", Caller(MyPromoteLevel.None), r);

            Assert.NotNull(captured);
            Assert.IsType<InvalidOperationException>(captured);
            Assert.Contains("kaboom", r.LastText);
        }

        [CommandRoot("test", "Override Plugin", "overrides a command")]
        public sealed class OverrideCommands : CommandModule
        {
            [Command("ping", "replies pong2")]
            [Permission(MyPromoteLevel.None)]
            public string Ping() => "pong2";
        }

        [CommandRoot("def", "Default Plugin", "has a default command")]
        public sealed class DefaultCommands : CommandModule
        {
            [Command("", "the default")]
            [Permission(MyPromoteLevel.None)]
            public string Root() => "default-ran";

            [Command("sub", "a sub command")]
            [Permission(MyPromoteLevel.None)]
            public string Sub() => "sub-ran";
        }

        [CommandRoot("adm", "Admin Default Plugin", "default needs admin")]
        public sealed class AdminDefaultCommands : CommandModule
        {
            // No [Permission] => defaults to Admin.
            [Command("", "admin default")]
            public string Root() => "admin-default-ran";
        }

        private static CommandDispatcher BuildDispatcherFor(Type moduleType, out CapturingResponder responder)
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(moduleType, "owner1");
            responder = new CapturingResponder();
            return new CommandDispatcher(registry, null);
        }

        [Fact]
        public void Handle_BarePrefix_WithDefault_RunsDefaultCommand()
        {
            var d = BuildDispatcherFor(typeof(DefaultCommands), out var r);
            bool handled = d.Handle("!def", Caller(MyPromoteLevel.None), r);

            Assert.True(handled);
            Assert.Equal("default-ran", Assert.Single(r.Replies).Text);
        }

        [Fact]
        public void Handle_BarePrefix_WithoutDefault_ShowsOverview()
        {
            // SampleCommands has no default command, so a bare prefix must still
            // print the auto-generated overview (regression guard).
            var d = BuildDispatcher(out var r);
            bool handled = d.Handle("!test", Caller(MyPromoteLevel.None), r);

            Assert.True(handled);
            string all = string.Join("\n", r.Texts);
            Assert.Contains("Test Plugin", all);
            Assert.Contains("ping", all);
        }

        [Fact]
        public void Handle_NamedSubcommand_StillResolves_WithDefaultPresent()
        {
            var d = BuildDispatcherFor(typeof(DefaultCommands), out var r);
            d.Handle("!def sub", Caller(MyPromoteLevel.None), r);
            Assert.Equal("sub-ran", r.LastText);
        }

        [Fact]
        public void Handle_BarePrefix_DefaultAboveCallerLevel_ShowsOverview()
        {
            // The default needs Admin; a non-admin bare call falls back to the
            // overview instead of running it.
            var d = BuildDispatcherFor(typeof(AdminDefaultCommands), out var r);
            d.Handle("!adm", Caller(MyPromoteLevel.None), r);

            string all = string.Join("\n", r.Texts);
            Assert.DoesNotContain("admin-default-ran", all);
            Assert.Contains("Admin Default Plugin", all);
        }

        [Fact]
        public void Handle_BarePrefix_DefaultAtCallerLevel_RunsIt()
        {
            var d = BuildDispatcherFor(typeof(AdminDefaultCommands), out var r);
            d.Handle("!adm", Caller(MyPromoteLevel.Admin), r);
            Assert.Equal("admin-default-ran", r.LastText);
        }

        [Fact]
        public void Register_ConflictingCommand_LastRegistrationWins()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(SampleCommands), "owner1");
            registry.RegisterModule(typeof(OverrideCommands), "owner2");

            var r = new CapturingResponder();
            var d = new CommandDispatcher(registry, null);
            d.Handle("!test ping", Caller(MyPromoteLevel.None), r);

            Assert.Equal("pong2", r.LastText);
        }
    }
}
