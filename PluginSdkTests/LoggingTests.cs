using System;
using System.Collections.Generic;
using System.Text.Json;
using PluginSdk.Logging;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Tests for the unified <see cref="Logger"/>: that it stamps every entry
    /// with the plugin name, level, managed thread id and UTC time and routes
    /// it to the sink, and that the two sinks render entries as required —
    /// <see cref="QuasarLogSink"/> as ISO 8601 microsecond-precision JSON, and
    /// <see cref="LogEnvironment"/> selecting the right sink per environment.
    /// </summary>
    public class LoggingTests
    {
        private sealed class CapturingSink : ILogSink
        {
            public readonly List<LogEntry> Entries = new List<LogEntry>();
            public void Write(in LogEntry entry) => Entries.Add(entry);
        }

        [Fact]
        public void Logger_StampsPluginNameLevelThreadAndUtcTime()
        {
            var sink = new CapturingSink();
            var log = new Logger("MyPlugin", sink);

            log.Warning("hello");

            var entry = Assert.Single(sink.Entries);
            Assert.Equal("MyPlugin", entry.PluginName);
            Assert.Equal(LogLevel.Warning, entry.Level);
            Assert.Equal("hello", entry.Message);
            Assert.Equal(Environment.CurrentManagedThreadId, entry.ThreadId);
            Assert.Equal(DateTimeKind.Utc, entry.UtcTimestamp.Kind);
            Assert.Null(entry.Exception);
        }

        [Fact]
        public void Logger_Error_CapturesException()
        {
            var sink = new CapturingSink();
            var log = new Logger("P", sink);
            var ex = new InvalidOperationException("boom");

            log.Error("failed", ex);

            var entry = Assert.Single(sink.Entries);
            Assert.Equal(LogLevel.Error, entry.Level);
            Assert.Same(ex, entry.Exception);
            Assert.Null(entry.Data);
        }

        [Fact]
        public void Logger_CapturesDataPayload()
        {
            var sink = new CapturingSink();
            var log = new Logger("P", sink);
            var payload = new { count = 42 };

            log.Info("downloaded", payload);

            var entry = Assert.Single(sink.Entries);
            Assert.Equal(LogLevel.Info, entry.Level);
            Assert.Same(payload, entry.Data);
            Assert.Null(entry.Exception);
        }

        [Fact]
        public void Logger_Error_CapturesExceptionAndData()
        {
            var sink = new CapturingSink();
            var log = new Logger("P", sink);
            var ex = new InvalidOperationException("boom");
            var payload = new { id = 7 };

            // A non-exception second argument binds to the data overload; an
            // exception binds to the exception overload, so both can coexist.
            log.Error("failed", ex, payload);

            var entry = Assert.Single(sink.Entries);
            Assert.Same(ex, entry.Exception);
            Assert.Same(payload, entry.Data);
        }

        [Fact]
        public void Logger_NullArguments_Throw()
        {
            var sink = new CapturingSink();
            Assert.Throws<ArgumentNullException>(() => new Logger(null, sink));
            Assert.Throws<ArgumentNullException>(() => new Logger("P", null));
        }

        [Fact]
        public void QuasarLogSink_FormatsIso8601MicrosecondUtcJson()
        {
            // 2026-05-30T12:34:56 + 1_234_560 ticks == .123456 s exactly.
            var ts = new DateTime(2026, 5, 30, 12, 34, 56, DateTimeKind.Utc).AddTicks(1_234_560);
            var entry = new LogEntry(ts, LogLevel.Info, "MyPlugin", 12, "Loaded", null, null);

            var json = QuasarLogSink.Format(in entry);

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            Assert.Equal("2026-05-30T12:34:56.123456Z", root.GetProperty("timestamp").GetString());
            Assert.Equal("Info", root.GetProperty("level").GetString());
            Assert.Equal("MyPlugin", root.GetProperty("plugin").GetString());
            Assert.Equal(12, root.GetProperty("thread").GetInt32());
            Assert.Equal("Loaded", root.GetProperty("message").GetString());
            Assert.False(root.TryGetProperty("data", out _));
            Assert.False(root.TryGetProperty("exception", out _));
        }

        [Fact]
        public void QuasarLogSink_NestsDataPayloadAsJsonObject()
        {
            var ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entry = new LogEntry(ts, LogLevel.Info, "P", 1, "downloaded",
                null, new { count = 42, name = "mods" });

            var json = QuasarLogSink.Format(in entry);

            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("data", out var data));
            Assert.Equal(JsonValueKind.Object, data.ValueKind);
            Assert.Equal(42, data.GetProperty("count").GetInt32());
            Assert.Equal("mods", data.GetProperty("name").GetString());
        }

        [Fact]
        public void MagnetarLogSink_AppendsDataPayloadAsJsonText()
        {
            var ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entry = new LogEntry(ts, LogLevel.Info, "MyPlugin", 7, "downloaded",
                null, new { count = 42 });

            var line = MagnetarLogSink.Format(in entry);

            Assert.Contains("[MyPlugin] [thread 7] downloaded", line);
            Assert.Contains("{\"count\":42}", line);
        }

        [Fact]
        public void QuasarLogSink_IncludesExceptionWhenPresent()
        {
            Exception ex;
            try { throw new InvalidOperationException("boom"); }
            catch (Exception caught) { ex = caught; }

            var ts = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var entry = new LogEntry(ts, LogLevel.Error, "P", 1, "failed", ex, null);

            var json = QuasarLogSink.Format(in entry);

            using var doc = JsonDocument.Parse(json);
            Assert.True(doc.RootElement.TryGetProperty("exception", out var exEl));
            Assert.Contains("InvalidOperationException", exEl.GetString());
        }

        [Fact]
        public void QuasarLogSink_WritesSingleJsonLineToInjectedWriter()
        {
            var lines = new List<string>();
            var sink = new QuasarLogSink(lines.Add);
            var entry = new LogEntry(
                new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                LogLevel.Debug, "P", 1, "x", null, null);

            sink.Write(in entry);

            var line = Assert.Single(lines);
            Assert.DoesNotContain("\n", line);
            Assert.StartsWith("{", line);
        }

        [Fact]
        public void LogEnvironment_SelectsSinkFromEnvironmentVariable()
        {
            var original = Environment.GetEnvironmentVariable(LogEnvironment.QuasarEnvironmentVariable);
            try
            {
                Environment.SetEnvironmentVariable(LogEnvironment.QuasarEnvironmentVariable, "1");
                Assert.True(LogEnvironment.IsManagedByQuasar());
                Assert.IsType<QuasarLogSink>(LogEnvironment.CreateDefaultSink());

                Environment.SetEnvironmentVariable(LogEnvironment.QuasarEnvironmentVariable, null);
                Assert.False(LogEnvironment.IsManagedByQuasar());
                Assert.IsType<MagnetarLogSink>(LogEnvironment.CreateDefaultSink());
            }
            finally
            {
                Environment.SetEnvironmentVariable(LogEnvironment.QuasarEnvironmentVariable, original);
            }
        }
    }
}
