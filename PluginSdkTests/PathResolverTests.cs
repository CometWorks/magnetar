using System.IO;
using PluginSdk.Paths;
using Xunit;

namespace PluginSdk.Tests
{
    /// <summary>
    /// Tests for the <see cref="PathResolver"/> facade: the default pass-through
    /// shim (correct on a case-insensitive OS) and backend installation, which
    /// is how the host wires in the LinuxCompat case-insensitive path cache.
    /// </summary>
    public class PathResolverTests
    {
        /// <summary>Records the calls routed to it and echoes a marker so tests
        /// can prove the facade forwarded to this backend rather than the shim.</summary>
        private sealed class FakeResolver : IPathResolver
        {
            public string LastCall;
            public string Normalize(string path) { LastCall = "Normalize"; return "N:" + path; }
            public string ToWindowsPath(string path) { LastCall = "ToWindowsPath"; return "W:" + path; }
            public string GetFileName(string path) { LastCall = "GetFileName"; return "F:" + path; }
            public string GetFileNameWithoutExtension(string path) { LastCall = "GetFileNameWithoutExtension"; return "E:" + path; }
            public string ResolveContentFilePath(string relativePath, string rootPath) { LastCall = "ResolveContentFilePath"; return "C:" + relativePath + "|" + rootPath; }
            public string ResolveAbsolute(string absolutePath) { LastCall = "ResolveAbsolute"; return "A:" + absolutePath; }
        }

        [Fact]
        public void Shim_IsActiveByDefault()
        {
            PathResolver.Install(null); // reset to shim
            Assert.False(PathResolver.IsCaseInsensitiveResolverActive);
        }

        [Fact]
        public void Shim_PassesPathsThroughUnchanged()
        {
            PathResolver.Install(null);

            Assert.Equal(@"Textures\Foo.dds", PathResolver.Normalize(@"Textures\Foo.dds"));
            Assert.Equal(@"C:\Game\Content", PathResolver.ToWindowsPath(@"C:\Game\Content"));
            Assert.Equal(@"X:\Game\file.sbc", PathResolver.ResolveAbsolute(@"X:\Game\file.sbc"));
        }

        [Fact]
        public void Shim_GetFileName_MatchesBcl()
        {
            PathResolver.Install(null);

            Assert.Equal(Path.GetFileName("dir/file.sbc"), PathResolver.GetFileName("dir/file.sbc"));
            Assert.Equal(
                Path.GetFileNameWithoutExtension("dir/file.sbc"),
                PathResolver.GetFileNameWithoutExtension("dir/file.sbc"));
        }

        [Fact]
        public void Shim_ResolveContentFilePath_CombinesWithRoot()
        {
            PathResolver.Install(null);

            Assert.Equal("rel.sbc", PathResolver.ResolveContentFilePath("rel.sbc", ""));
            Assert.Equal(
                Path.Combine("root", "rel.sbc"),
                PathResolver.ResolveContentFilePath("rel.sbc", "root"));
        }

        [Fact]
        public void Install_RoutesCallsToBackend_AndReportsActive()
        {
            FakeResolver fake = new FakeResolver();
            try
            {
                PathResolver.Install(fake);

                Assert.True(PathResolver.IsCaseInsensitiveResolverActive);
                Assert.Equal("N:p", PathResolver.Normalize("p"));
                Assert.Equal("W:p", PathResolver.ToWindowsPath("p"));
                Assert.Equal("F:p", PathResolver.GetFileName("p"));
                Assert.Equal("E:p", PathResolver.GetFileNameWithoutExtension("p"));
                Assert.Equal("C:rel|root", PathResolver.ResolveContentFilePath("rel", "root"));
                Assert.Equal("A:p", PathResolver.ResolveAbsolute("p"));
            }
            finally
            {
                PathResolver.Install(null); // restore shim for other tests
            }
        }
    }
}
