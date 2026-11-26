using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Threading.Tasks;
using PluginSdk.Commands;
using VRage.Game.ModAPI;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Registering, overriding and removing commands, including from several
    /// threads while commands run.
    /// </summary>
    public class CommandRegistryTests
    {
        /// <summary>Collects the replies a command sends. Thread-safe, because the
        /// concurrency tests run commands from a background thread.</summary>
        private sealed class Capture : ICommandResponder
        {
            public readonly List<string> Texts = new List<string>();
            private readonly object gate = new object();

            public void Send(in CommandReply reply, in CommandCaller caller)
            {
                lock (gate)
                    Texts.Add(reply.Text);
            }

            public string Last
            {
                get
                {
                    lock (gate)
                        return Texts.Count == 0 ? null : Texts[Texts.Count - 1];
                }
            }
        }

        /// <summary>An Admin caller, which may run every fixture command that has no explicit level.</summary>
        private static CommandCaller Caller() =>
            new CommandCaller(1UL, 1L, "Tester", MyPromoteLevel.Admin);

        /// <summary>Runs a chat line as an Admin and returns the last reply. <paramref name="handled"/>
        /// is false when the line named no registered command.</summary>
        private static string Run(CommandRegistry registry, string line, out bool handled)
        {
            var capture = new Capture();
            handled = new CommandDispatcher(registry).Handle(line, Caller(), capture);
            return capture.Last;
        }

        /// <summary>Runs a chat line as a caller of the given level and returns the last reply.</summary>
        private static string RunAs(CommandRegistry registry, string line, MyPromoteLevel level)
        {
            var capture = new Capture();
            new CommandDispatcher(registry).Handle(
                line,
                new CommandCaller(2UL, 2L, "Player", level),
                capture
            );
            return capture.Last;
        }

        // ---- Fixture modules ------------------------------------------------------
        // Prefixes start with "reg" so they never collide with other test files. Each
        // command returns a value that says which module ran.

        [CommandRoot("rega", "A", "first")]
        public sealed class ModA : CommandModule
        {
            [Command("ping", "pong")]
            public string Ping() => "a";
        }

        [CommandRoot("regb", "B", "second")]
        public sealed class ModB : CommandModule
        {
            [Command("ping", "pong")]
            public string Ping() => "b";
        }

        // Three modules on one prefix that all define "ping", to test override order
        // and restore. OverBase and OverTop also define the root-level default.
        [CommandRoot("regov", "Over", "base")]
        public sealed class OverBase : CommandModule
        {
            [Command("ping", "pong")]
            public string Ping() => "base";

            [Command("only", "base only")]
            public string Only() => "only-base";

            [Command("", "default")]
            public string Default() => "default-base";
        }

        [CommandRoot("regov", "Over", "override")]
        public sealed class OverTop : CommandModule
        {
            [Command("ping", "pong")]
            public string Ping() => "top";

            [Command("", "default")]
            public string Default() => "default-top";
        }

        [CommandRoot("regov", "Over", "third")]
        public sealed class OverThird : CommandModule
        {
            [Command("ping", "pong")]
            public string Ping() => "third";
        }

        // The same path with different permission levels, to check that the active
        // command's own level is the one applied.
        [CommandRoot("regperm", "Perm", "permissions")]
        public sealed class PermLow : CommandModule
        {
            [Command("act", "low")]
            [Permission(MyPromoteLevel.None)]
            public string Act() => "low";
        }

        [CommandRoot("regperm", "Perm", "permissions")]
        public sealed class PermHigh : CommandModule
        {
            [Command("act", "high")]
            [Permission(MyPromoteLevel.Owner)]
            public string Act() => "high";
        }

        // A module with a heading and no commands, and another on the same prefix
        // that has one, to test roots that hold only a heading.
        [CommandRoot("regempty", "Empty", "no commands")]
        public sealed class EmptyModule : CommandModule { }

        [CommandRoot("regempty", "Other", "with a command")]
        public sealed class EmptyRootWithCommand : CommandModule
        {
            [Command("ping", "pong")]
            public string Ping() => "ping";
        }

        // Two modules that add "grid list" and "grid add" under the same "grid"
        // namespace, to test that removing one keeps the other and that an emptied
        // namespace is dropped.
        [CommandRoot("regnest", "Nest", "nested")]
        public sealed class NestList : CommandModule
        {
            [Command("grid list", "lists")]
            public string List() => "list";
        }

        [CommandRoot("regnest", "Nest", "nested")]
        public sealed class NestAdd : CommandModule
        {
            [Command("grid add", "adds")]
            public string Add() => "add";
        }

        // Two owners on one prefix with different headings.
        [CommandRoot("regshared", "First", "first")]
        public sealed class SharedFirst : CommandModule
        {
            [Command("one", "one")]
            public string One() => "1";
        }

        [CommandRoot("regshared", "Second", "second")]
        public sealed class SharedSecond : CommandModule
        {
            [Command("two", "two")]
            public string Two() => "2";
        }

        // Invalid: "help" is a reserved word in a command path, so registering this
        // module must be refused as a whole, including its valid "fine" command.
        // Two reserved "help" commands in one module, to check that every bad command
        // in a module is reported, not only the first.
        [CommandRoot("regbadtwo", "Bad", "two invalid commands")]
        public sealed class TwoBadCommands : CommandModule
        {
            [Command("help a", "reserved word")]
            public string First() => "no";

            [Command("help b", "reserved word")]
            public string Second() => "no";
        }

        // Module-level problems: no [CommandRoot], the reserved prefix, a prefix with a space.
        public sealed class NoRootModule : CommandModule { }

        [CommandRoot("help", "Reserved", "reserved prefix")]
        public sealed class ReservedPrefixModule : CommandModule { }

        [CommandRoot("two words", "Spaced", "invalid prefix")]
        public sealed class SpacedPrefixModule : CommandModule { }

        [CommandRoot("regbad", "Bad", "invalid")]
        public sealed class BadModule : CommandModule
        {
            [Command("fine", "fine")]
            public string Fine() => "fine";

            [Command("help me", "reserved word")]
            public string Reserved() => "no";
        }

        /// <summary>
        /// A module with one invalid command registers none of its commands and leaves no empty root behind.
        /// </summary>
        [Fact]
        public void FailedModule_RegistersNothing()
        {
            var registry = new CommandRegistry();

            Assert.Throws<CommandRegistrationException>(() =>
                registry.RegisterModule(typeof(BadModule), "owner")
            );

            Assert.DoesNotContain("regbad", registry.Prefixes);
            Run(registry, "!regbad fine", out bool handled);
            Assert.False(handled);
        }

        /// <summary>
        /// Removing one owner leaves another owner's commands and root in place.
        /// </summary>
        [Fact]
        public void Unregister_RemovesOnlyThatOwner()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(ModA), "one");
            registry.RegisterModule(typeof(ModB), "two");

            Assert.Equal(1, registry.Unregister("one"));

            Assert.DoesNotContain("rega", registry.Prefixes);
            Assert.Contains("regb", registry.Prefixes);
            Run(registry, "!rega ping", out bool handledA);
            Assert.False(handledA);
            Assert.Equal("b", Run(registry, "!regb ping", out _));
        }

        /// <summary>
        /// Removing an owner that overrode a command brings the earlier command back, including the default command.
        /// </summary>
        [Fact]
        public void Unregister_RestoresWhatTheOwnerOverrode()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(OverBase), "base");
            registry.RegisterModule(typeof(OverTop), "top");
            Assert.Equal("top", Run(registry, "!regov ping", out _));
            Assert.Equal("default-top", Run(registry, "!regov", out _));
            Assert.Equal("only-base", Run(registry, "!regov only", out _));

            Assert.Equal(2, registry.Unregister("top"));

            Assert.Equal("base", Run(registry, "!regov ping", out _));
            Assert.Equal("default-base", Run(registry, "!regov", out _));
        }

        /// <summary>
        /// When the last owner is removed, the root is gone and its commands are unknown.
        /// </summary>
        [Fact]
        public void Unregister_LastOwner_DropsTheRoot()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(OverBase), "base");
            registry.RegisterModule(typeof(OverTop), "top");

            registry.Unregister("top");
            registry.Unregister("base");

            Assert.DoesNotContain("regov", registry.Prefixes);
            Run(registry, "!regov ping", out bool handled);
            Assert.False(handled);
        }

        /// <summary>
        /// Removing "grid list" keeps "grid add" and drops the namespace once both are gone.
        /// </summary>
        [Fact]
        public void Unregister_PrunesEmptyNamespacesAndKeepsSiblings()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(NestList), "list");
            registry.RegisterModule(typeof(NestAdd), "add");

            registry.Unregister("list");

            Assert.Equal("add", Run(registry, "!regnest grid add", out _));
            Assert.Contains("Unknown", Run(registry, "!regnest grid list", out _));

            registry.Unregister("add");
            Assert.DoesNotContain("regnest", registry.Prefixes);
        }

        /// <summary>
        /// The title and description shown follow the earliest owner still registered.
        /// </summary>
        [Fact]
        public void Unregister_HeadingFallsBackToTheNextOwner()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(SharedFirst), "first");
            registry.RegisterModule(typeof(SharedSecond), "second");
            Assert.True(registry.TryGetRoot("regshared", out CommandRoot root));
            Assert.Equal("First", root.Title);

            registry.Unregister("first");

            Assert.Equal("Second", root.Title);
            Assert.Equal("second", root.Description);
        }

        /// <summary>
        /// Registering the same module twice under one owner leaves one entry, not two.
        /// </summary>
        [Fact]
        public void Register_SameOwnerAgain_ReplacesItsOwnEntries()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(ModA), "one");
            registry.RegisterModule(typeof(ModA), "one");

            Assert.Equal(1, registry.Unregister("one"));
            Assert.DoesNotContain("rega", registry.Prefixes);
        }

        /// <summary>
        /// Two different assemblies that share a name are separate owners: removing one leaves the other.
        /// </summary>
        [Fact]
        public void Unregister_AssemblyOwner_IsByAssemblyNotByName()
        {
            AssemblyBuilder first = Dynamic("SameName");
            AssemblyBuilder second = Dynamic("SameName");
            Assert.NotSame(first, second);
            Assert.Equal(first.GetName().Name, second.GetName().Name);

            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(ModA), first);
            registry.RegisterModule(typeof(ModB), second);

            Assert.Equal(1, registry.Unregister(first));

            Assert.DoesNotContain("rega", registry.Prefixes);
            Assert.Contains("regb", registry.Prefixes);
            Assert.Equal("b", Run(registry, "!regb ping", out _));
        }

        /// <summary>
        /// Removing an owner that registered nothing changes nothing.
        /// </summary>
        [Fact]
        public void Unregister_UnknownOwner_RemovesNothing()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(ModA), "one");

            Assert.Equal(0, registry.Unregister("nobody"));
            Assert.Equal("a", Run(registry, "!rega ping", out _));
        }

        /// <summary>
        /// The global !help listing drops a root once its owner is removed.
        /// </summary>
        [Fact]
        public void Help_AfterUnregister_NoLongerListsTheRoot()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(ModA), "one");
            registry.RegisterModule(typeof(ModB), "two");
            registry.Unregister("one");

            var capture = new Capture();
            new CommandDispatcher(registry).Handle("!help", Caller(), capture);

            string all = string.Join("\n", capture.Texts);
            Assert.DoesNotContain("!rega", all);
            Assert.Contains("!regb", all);
        }

        /// <summary>
        /// One invalid module in an assembly means none of that assembly's modules are registered.
        /// </summary>
        [Fact]
        public void RegisterAssembly_OneInvalidModule_RegistersNothingFromTheAssembly()
        {
            AssemblyBuilder assembly = EmitAssembly(
                ("asmgood", "ping"),
                ("asmbad", "help me")
            );
            var registry = new CommandRegistry();

            Assert.Throws<CommandRegistrationException>(() => registry.RegisterAssembly(assembly));

            Assert.Empty(registry.Prefixes);
        }

        /// <summary>
        /// Every module of an assembly registers, and removing the assembly removes them all.
        /// </summary>
        [Fact]
        public void RegisterAssembly_ValidModules_AllRegisterAndUnregisterTogether()
        {
            AssemblyBuilder assembly = EmitAssembly(("asmone", "ping"), ("asmtwo", "ping"));
            var registry = new CommandRegistry();

            Assert.Equal(2, registry.RegisterAssembly(assembly));
            Assert.Equal(2, registry.Prefixes.Count);

            Assert.Equal(2, registry.Unregister(assembly));
            Assert.Empty(registry.Prefixes);
        }

        /// <summary>
        /// An owner with several modules on one prefix shows the first module's title and description.
        /// </summary>
        [Fact]
        public void SameOwner_SeveralModulesOnOnePrefix_KeepTheFirstHeading()
        {
            AssemblyBuilder assembly = EmitAssembly(
                ("asmshared", "one", "First", "first description"),
                ("asmshared", "two", null, null)
            );
            var registry = new CommandRegistry();
            registry.RegisterModule(assembly.GetType("Module0"), assembly);
            registry.RegisterModule(assembly.GetType("Module1"), assembly);

            Assert.True(registry.TryGetRoot("asmshared", out CommandRoot root));
            Assert.Equal("First", root.Title);
            Assert.Equal("first description", root.Description);
        }

        /// <summary>
        /// A null owner string is the same owner as the empty string, so it can be registered and removed.
        /// </summary>
        [Fact]
        public void NullOwner_IsTreatedAsTheEmptyOwner()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(ModA), (string)null);
            registry.RegisterModule(typeof(ModB), (string)null);
            registry.RegisterModule(typeof(ModA), "x");

            Assert.Equal(2, registry.Unregister((string)null));
            Assert.Equal("a", Run(registry, "!rega ping", out _));
            Assert.DoesNotContain("regb", registry.Prefixes);
        }

        /// <summary>
        /// Every problem in an assembly is reported in one exception, and nothing is registered.
        /// </summary>
        [Fact]
        public void RegisterAssembly_SeveralProblems_AreAllReportedTogether()
        {
            AssemblyBuilder assembly = EmitAssembly(
                ("allbadone", "help a"),
                ("asmfine", "ping"),
                ("allbadtwo", "help b")
            );
            var registry = new CommandRegistry();

            var error = Assert.Throws<CommandRegistrationException>(() =>
                registry.RegisterAssembly(assembly)
            );

            Assert.Contains("2 problems", error.Message);
            Assert.Contains("!allbadone help", error.Message);
            Assert.Contains("!allbadtwo help", error.Message);
            Assert.Empty(registry.Prefixes);
        }

        /// <summary>
        /// One problem keeps its own plain message, with no problem count added.
        /// </summary>
        [Fact]
        public void RegisterAssembly_OneProblem_KeepsItsPlainMessage()
        {
            AssemblyBuilder assembly = EmitAssembly(("onlybad", "help a"));

            var error = Assert.Throws<CommandRegistrationException>(() =>
                new CommandRegistry().RegisterAssembly(assembly)
            );

            Assert.DoesNotContain("problems", error.Message);
            Assert.Contains("'!onlybad help' is reserved", error.Message);
        }

        /// <summary>
        /// Every invalid command in one module is reported together, and none is registered.
        /// </summary>
        [Fact]
        public void RegisterModule_TwoBadCommands_AreBothReported()
        {
            var registry = new CommandRegistry();

            var error = Assert.Throws<CommandRegistrationException>(() =>
                registry.RegisterModule(typeof(TwoBadCommands), "owner")
            );

            Assert.StartsWith("2 problems:", error.Message);
            Assert.Contains("TwoBadCommands.First", error.Message);
            Assert.Contains("TwoBadCommands.Second", error.Message);
            Assert.Empty(registry.Prefixes);
        }

        /// <summary>
        /// A problem with the module as a whole is refused with its own message and registers nothing.
        /// </summary>
        [Fact]
        public void RegisterModule_ModuleLevelProblems_AreRefusedWithTheirOwnMessage()
        {
            var cases = new[]
            {
                (typeof(string), "is not a CommandModule"),
                (typeof(NoRootModule), "has no [CommandRoot]"),
                (typeof(ReservedPrefixModule), "uses the reserved prefix 'help'"),
                (typeof(SpacedPrefixModule), "has an invalid command prefix 'two words'"),
            };

            foreach ((Type module, string expected) in cases)
            {
                var registry = new CommandRegistry();

                var error = Assert.Throws<CommandRegistrationException>(() =>
                    registry.RegisterModule(module, "owner")
                );

                Assert.Contains(expected, error.Message);
                Assert.DoesNotContain("problems", error.Message);
                Assert.Empty(registry.Prefixes);
            }
        }

        /// <summary>
        /// A null assembly is refused with ArgumentNullException.
        /// </summary>
        [Fact]
        public void RegisterAssembly_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() =>
                new CommandRegistry().RegisterAssembly(null)
            );
        }

        /// <summary>
        /// A null module type is refused with ArgumentNullException naming the argument, not a
        /// NullReferenceException, on both RegisterModule overloads, and registers nothing.
        /// </summary>
        [Fact]
        public void RegisterModule_NullType_Throws()
        {
            var registry = new CommandRegistry();

            var byOwner = Assert.Throws<ArgumentNullException>(() =>
                registry.RegisterModule(null, "owner")
            );
            var byAssembly = Assert.Throws<ArgumentNullException>(() =>
                registry.RegisterModule(null, typeof(ModA).Assembly)
            );

            Assert.Equal("moduleType", byOwner.ParamName);
            Assert.Equal("moduleType", byAssembly.ParamName);
            Assert.Empty(registry.Prefixes);
        }

        /// <summary>
        /// With three owners at one path, removing the top ones restores the next most recent each time.
        /// </summary>
        [Fact]
        public void Unregister_ThreeOwnersAtOnePath_RestoresInOrder()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(OverBase), "a");
            registry.RegisterModule(typeof(OverTop), "b");
            registry.RegisterModule(typeof(OverThird), "c");
            Assert.Equal("third", Run(registry, "!regov ping", out _));

            registry.Unregister("c");
            Assert.Equal("top", Run(registry, "!regov ping", out _));

            registry.Unregister("b");
            Assert.Equal("base", Run(registry, "!regov ping", out _));
        }

        /// <summary>
        /// Removing an owner whose command is overridden does not change which command runs.
        /// </summary>
        [Fact]
        public void Unregister_MiddleOwner_LeavesTheActiveOne()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(OverBase), "a");
            registry.RegisterModule(typeof(OverTop), "b");
            registry.RegisterModule(typeof(OverThird), "c");

            registry.Unregister("b");

            Assert.Equal("third", Run(registry, "!regov ping", out _));
            registry.Unregister("c");
            Assert.Equal("base", Run(registry, "!regov ping", out _));
        }

        /// <summary>
        /// An owner that registers again after being overridden becomes the active one, and removing it restores the next.
        /// </summary>
        [Fact]
        public void Register_EarlierOwnerAgain_BecomesTheActiveOne()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(OverBase), "a");
            registry.RegisterModule(typeof(OverThird), "c");
            registry.RegisterModule(typeof(OverBase), "a");
            Assert.Equal("base", Run(registry, "!regov ping", out _));

            registry.Unregister("a");

            Assert.Equal("third", Run(registry, "!regov ping", out _));
        }

        /// <summary>
        /// A caller below a command's level is refused, whichever owner's command is active, and each command keeps its own level.
        /// </summary>
        [Fact]
        public void Permissions_FollowTheActiveCommand_AcrossOverrideAndRestore()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(PermLow), "low");
            Assert.Equal("low", RunAs(registry, "!regperm act", MyPromoteLevel.None));

            // A stricter command overrides it: the caller below its level is refused.
            registry.RegisterModule(typeof(PermHigh), "high");
            Assert.Contains("permission", RunAs(registry, "!regperm act", MyPromoteLevel.None));
            Assert.Equal("high", RunAs(registry, "!regperm act", MyPromoteLevel.Owner));

            // Removing it brings the original back at its own level.
            registry.Unregister("high");
            Assert.Equal("low", RunAs(registry, "!regperm act", MyPromoteLevel.None));
        }

        /// <summary>
        /// The global !help shows a root only when the caller may use at least one of its commands.
        /// </summary>
        [Fact]
        public void Help_ListsOnlyRootsTheCallerMayUse()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(PermLow), "low");
            registry.RegisterModule(typeof(ModA), "admin-only");

            var capture = new Capture();
            new CommandDispatcher(registry).Handle(
                "!help",
                new CommandCaller(2UL, 2L, "Player", MyPromoteLevel.None),
                capture
            );

            string all = string.Join("\n", capture.Texts);
            Assert.Contains("!regperm", all);
            Assert.DoesNotContain("!rega", all);
        }

        /// <summary>
        /// Removing one owner's commands does not drop a root that another owner registered with only a heading.
        /// </summary>
        [Fact]
        public void Unregister_AnotherOwnersCommands_KeepsTheHeadingOnlyOwnersRoot()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(EmptyModule), "heading");
            registry.RegisterModule(typeof(EmptyRootWithCommand), "commands");

            registry.Unregister("commands");

            Assert.Contains("regempty", registry.Prefixes);
            Assert.True(registry.TryGetRoot("regempty", out CommandRoot root));
            Assert.Equal("Empty", root.Title);

            registry.Unregister("heading");
            Assert.DoesNotContain("regempty", registry.Prefixes);
        }

        /// <summary>
        /// An unrelated removal leaves a root that has only a heading alone.
        /// </summary>
        [Fact]
        public void Unregister_KeepsARootThatHasOnlyAHeading()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(EmptyModule), "heading");
            registry.RegisterModule(typeof(ModA), "other");

            registry.Unregister("other");

            Assert.Contains("regempty", registry.Prefixes);
            Assert.DoesNotContain("rega", registry.Prefixes);
        }

        /// <summary>
        /// Several threads register the same modules under different owners at once; every owner's commands are present, and each can then be removed exactly once.
        /// </summary>
        [Fact]
        public void ConcurrentRegistration_KeepsEveryOwnersCommands()
        {
            const int perThread = 400;
            const int threads = 4;
            var registry = new CommandRegistry();
            var errors = new List<Exception>();

            Parallel.For(
                0,
                threads,
                new ParallelOptions { MaxDegreeOfParallelism = threads },
                t =>
                {
                    try
                    {
                        for (int i = 0; i < perThread; i++)
                        {
                            registry.RegisterModule(typeof(ModA), $"a-{t}-{i}");
                            registry.RegisterModule(typeof(ModB), $"b-{t}-{i}");
                            registry.RegisterModule(typeof(NestList), $"n-{t}-{i}");
                            registry.RegisterModule(typeof(NestAdd), $"m-{t}-{i}");
                        }
                    }
                    catch (Exception e)
                    {
                        lock (errors)
                            errors.Add(e);
                    }
                }
            );

            Assert.Empty(errors);
            Assert.Equal(3, registry.Prefixes.Count);
            Assert.Equal("a", Run(registry, "!rega ping", out _));
            Assert.Equal("b", Run(registry, "!regb ping", out _));
            Assert.Equal("list", Run(registry, "!regnest grid list", out _));
            Assert.Equal("add", Run(registry, "!regnest grid add", out _));

            // Every owner registered its one command, so each removal finds exactly
            // one and the registry ends up empty.
            int missing = 0;
            for (int t = 0; t < threads; t++)
                for (int i = 0; i < perThread; i++)
                {
                    if (registry.Unregister($"a-{t}-{i}") != 1) missing++;
                    if (registry.Unregister($"b-{t}-{i}") != 1) missing++;
                    if (registry.Unregister($"n-{t}-{i}") != 1) missing++;
                    if (registry.Unregister($"m-{t}-{i}") != 1) missing++;
                }
            Assert.Equal(0, missing);
            Assert.Empty(registry.Prefixes);
        }

        /// <summary>
        /// A writer registers and removes a command while a reader runs commands and help. The reader never fails, and the command it finds is always a valid one.
        /// It guards the lock: it fails when the lock is removed. It does not reproduce the original race, because the original has no Unregister to churn.
        /// </summary>
        [Fact]
        public async Task RegisterAndUnregister_WhileCommandsRun_NeverBreaksTheReader()
        {
            var registry = new CommandRegistry();
            registry.RegisterModule(typeof(ModB), "stable");
            registry.RegisterModule(typeof(OverBase), "base");
            var errors = new List<Exception>();
            using var stop = new CancellationTokenSource();

            Task reader = Task.Run(() =>
            {
                var dispatcher = new CommandDispatcher(registry);
                try
                {
                    while (!stop.IsCancellationRequested)
                    {
                        var capture = new Capture();
                        dispatcher.Handle("!regb ping", Caller(), capture);
                        Assert.Equal("b", capture.Last);

                        // The overriding owner comes and goes. Either answer is
                        // valid, but a command that exists must always run.
                        capture = new Capture();
                        Assert.True(dispatcher.Handle("!regov ping", Caller(), capture));
                        Assert.Contains(capture.Last, new[] { "base", "top" });

                        dispatcher.Handle("!help", Caller(), new Capture());
                        dispatcher.Handle("!regov help", Caller(), new Capture());
                    }
                }
                catch (Exception e)
                {
                    lock (errors)
                        errors.Add(e);
                }
            });

            Task writer = Task.Run(() =>
            {
                try
                {
                    for (int i = 0; i < 3000; i++)
                    {
                        registry.RegisterModule(typeof(OverTop), "top");
                        registry.RegisterModule(typeof(ModA), $"churn-{i % 7}");
                        registry.Unregister("top");
                        registry.Unregister($"churn-{i % 7}");
                    }
                }
                catch (Exception e)
                {
                    lock (errors)
                        errors.Add(e);
                }
            });

            Assert.True(
                await Task.WhenAny(writer, Task.Delay(TimeSpan.FromSeconds(60))) == writer,
                "writer did not finish"
            );
            stop.Cancel();
            Assert.True(
                await Task.WhenAny(reader, Task.Delay(TimeSpan.FromSeconds(30))) == reader,
                "reader did not finish"
            );
            await writer;
            await reader;

            Assert.Empty(errors);
        }

        /// <summary>
        /// Two threads, released together, register 400 modules with distinct prefixes. This is the case where an unguarded root map loses roots, throws or hangs on the original code.
        /// </summary>
        [Fact]
        public async Task ConcurrentRegistration_ManyPrefixes_LosesNothing()
        {
            // Many distinct prefixes make the root map grow while another thread
            // writes it, which is where an unguarded map loses roots, throws, or
            // spins forever. Both threads start together on a barrier, and a hang
            // shows up as a timeout, not a stuck test run.
            List<Type> modules = EmitModules(400);
            for (int run = 0; run < 40; run++)
            {
                var registry = new CommandRegistry();
                Exception first = null;
                using (var start = new Barrier(2))
                {
                    Task[] workers = Enumerable
                        .Range(0, 2)
                        .Select(t =>
                            Task.Factory.StartNew(
                                () =>
                                {
                                    try
                                    {
                                        start.SignalAndWait();
                                        foreach (Type module in modules)
                                            registry.RegisterModule(module, "owner" + t);
                                    }
                                    catch (Exception e)
                                    {
                                        Interlocked.CompareExchange(ref first, e, null);
                                    }
                                },
                                TaskCreationOptions.LongRunning
                            )
                        )
                        .ToArray();
                    Task work = Task.WhenAll(workers);

                    Assert.True(
                        await Task.WhenAny(work, Task.Delay(TimeSpan.FromSeconds(20))) == work,
                        $"run {run} did not finish"
                    );
                }

                Assert.True(first == null, first?.ToString());
                Assert.Equal(modules.Count, registry.Prefixes.Count);
            }
        }

#if NET5_0_OR_GREATER
        /// <summary>
        /// Registration allocates a bounded amount per command, so the startup cost does not grow with the number of plugins. Only measurable on .NET 5 and later.
        /// </summary>
        [Fact]
        public void Registering_AllocationPerCommandStaysBounded()
        {
            // Registration is a startup cost: 20 plugins of 30 commands, each on
            // its own prefix. The bound leaves headroom over the measured figure
            // and catches a change that makes it grow.
            var modules = new List<Type>();
            for (int p = 0; p < 20; p++)
                modules.Add(EmitModules(1, "alloc" + p, 30)[0]);
            var warm = new CommandRegistry();
            foreach (Type module in modules)
                warm.RegisterModule(module, "warm");

            var registry = new CommandRegistry();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int p = 0; p < modules.Count; p++)
                registry.RegisterModule(modules[p], "owner" + p);
            long perCommand = (GC.GetAllocatedBytesForCurrentThread() - before) / (20 * 30);

            Assert.True(perCommand < 2000, $"{perCommand} bytes per command");
        }
#endif

        /// <summary>
        /// Builds <paramref name="count"/> command module types in a new dynamic assembly,
        /// each with <paramref name="commands"/> commands. With one module the prefix is
        /// <paramref name="prefix"/> itself, otherwise <paramref name="prefix"/> plus the index.
        /// </summary>
        private static List<Type> EmitModules(int count, string prefix = "race", int commands = 1)
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("EmittedModules" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run
            );
            ModuleBuilder module = assembly.DefineDynamicModule("m");
            ConstructorInfo rootCtor = typeof(CommandRootAttribute).GetConstructor(
                new[] { typeof(string), typeof(string), typeof(string) }
            );
            ConstructorInfo commandCtor = typeof(CommandAttribute).GetConstructor(
                new[] { typeof(string), typeof(string), typeof(string) }
            );

            var types = new List<Type>();
            for (int i = 0; i < count; i++)
            {
                TypeBuilder type = module.DefineType(
                    "Module" + i,
                    TypeAttributes.Public | TypeAttributes.Sealed,
                    typeof(CommandModule)
                );
                type.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        rootCtor,
                        new object[] { count == 1 ? prefix : prefix + i, "Title", "Description" }
                    )
                );
                for (int c = 0; c < commands; c++)
                {
                    MethodBuilder method = type.DefineMethod(
                        "Run" + c,
                        MethodAttributes.Public,
                        typeof(string),
                        Type.EmptyTypes
                    );
                    method.SetCustomAttribute(
                        new CustomAttributeBuilder(
                            commandCtor,
                            new object[] { commands == 1 ? "ping" : "group" + (c % 5) + " c" + c, "d", null }
                        )
                    );
                    ILGenerator il = method.GetILGenerator();
                    il.Emit(OpCodes.Ldstr, "x");
                    il.Emit(OpCodes.Ret);
                }
                types.Add(type.CreateType());
            }
            return types;
        }

        /// <summary>Builds a dynamic assembly with one module per (prefix, command) pair.</summary>
        private static AssemblyBuilder EmitAssembly(
            params (string Prefix, string Command)[] modules
        ) =>
            EmitAssembly(
                modules.Select(m => (m.Prefix, m.Command, "Title", "Description")).ToArray()
            );

        /// <summary>
        /// Builds a dynamic assembly with one module per entry. The types are named
        /// Module0, Module1, and so on, in order, with one command each.
        /// </summary>
        private static AssemblyBuilder EmitAssembly(
            params (string Prefix, string Command, string Title, string Description)[] modules
        )
        {
            AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("EmittedAssembly" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run
            );
            ModuleBuilder module = assembly.DefineDynamicModule("m");
            ConstructorInfo rootCtor = typeof(CommandRootAttribute).GetConstructor(
                new[] { typeof(string), typeof(string), typeof(string) }
            );
            ConstructorInfo commandCtor = typeof(CommandAttribute).GetConstructor(
                new[] { typeof(string), typeof(string), typeof(string) }
            );

            for (int i = 0; i < modules.Length; i++)
            {
                TypeBuilder type = module.DefineType(
                    "Module" + i,
                    TypeAttributes.Public | TypeAttributes.Sealed,
                    typeof(CommandModule)
                );
                type.SetCustomAttribute(
                    new CustomAttributeBuilder(
                        rootCtor,
                        new object[] { modules[i].Prefix, modules[i].Title, modules[i].Description }
                    )
                );
                MethodBuilder method = type.DefineMethod(
                    "Run",
                    MethodAttributes.Public,
                    typeof(string),
                    Type.EmptyTypes
                );
                method.SetCustomAttribute(
                    new CustomAttributeBuilder(commandCtor, new object[] { modules[i].Command, "d", null })
                );
                ILGenerator il = method.GetILGenerator();
                il.Emit(OpCodes.Ldstr, "x");
                il.Emit(OpCodes.Ret);
                type.CreateType();
            }
            return assembly;
        }

        /// <summary>An empty dynamic assembly with the given name. Two of these with the same
        /// name are different assemblies.</summary>
        private static AssemblyBuilder Dynamic(string name) =>
            AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
    }
}
