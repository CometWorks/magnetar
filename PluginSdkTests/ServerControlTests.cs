using PluginSdk;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Tests for the plugin-facing <see cref="ServerControl"/> facade: that
    /// <see cref="ServerControl.Bind"/> routes each public call to the supplied
    /// host delegate, and that binding nulls restores the safe no-op defaults
    /// (the <c>bool</c>-returning ones report <c>false</c>, the others do
    /// nothing). The facade is static global state, so each test rebinds rather
    /// than relying on the pre-Bind default.
    /// </summary>
    public class ServerControlTests
    {
        [Fact]
        public void Bind_RoutesEachCallToSuppliedDelegate()
        {
            int saveWorld = 0, reloadConfig = 0, saveAndQuit = 0;
            int saveAndRestart = 0, quitWithoutSaving = 0, restartWithoutSaving = 0;

            ServerControl.Bind(
                () => { saveWorld++; return true; },
                () => { reloadConfig++; return true; },
                () => saveAndQuit++,
                () => saveAndRestart++,
                () => quitWithoutSaving++,
                () => restartWithoutSaving++);

            Assert.True(ServerControl.SaveWorld());
            Assert.True(ServerControl.ReloadConfig());
            ServerControl.SaveAndQuit();
            ServerControl.SaveAndRestart();
            ServerControl.QuitWithoutSaving();
            ServerControl.RestartWithoutSaving();

            Assert.Equal(1, saveWorld);
            Assert.Equal(1, reloadConfig);
            Assert.Equal(1, saveAndQuit);
            Assert.Equal(1, saveAndRestart);
            Assert.Equal(1, quitWithoutSaving);
            Assert.Equal(1, restartWithoutSaving);
        }

        [Fact]
        public void Bind_WithNulls_RestoresSafeNoOps()
        {
            // First bind real delegates, then bind nulls to prove the nulls win.
            ServerControl.Bind(() => true, () => true, null, null, null, null);
            ServerControl.Bind(null, null, null, null, null, null);

            Assert.False(ServerControl.SaveWorld());
            Assert.False(ServerControl.ReloadConfig());

            // The void calls must not throw with no implementation bound.
            ServerControl.SaveAndQuit();
            ServerControl.SaveAndRestart();
            ServerControl.QuitWithoutSaving();
            ServerControl.RestartWithoutSaving();
        }
    }
}
