// PluginSdk contract version, major.minor. It is separate from the Magnetar
// version and changes only when the public API does:
//   minor - public API added
//   major - public API removed or changed, or documented behavior plugins
//           rely on changed incompatibly
// A plugin built against M.m runs on any PluginSdk with major M and minor >= m.
// Cluster records it at build time and Quasar compares it instead of the file
// hash, so Magnetar releases without an API change need no cluster re-pin.
// Build/check-plugin-sdk-contract.py enforces the bump against the API files.
[assembly: System.Reflection.AssemblyMetadata("PluginSdkContract", "1.0")]
