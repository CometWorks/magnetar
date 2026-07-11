#if NETFRAMEWORK
namespace System.Runtime.CompilerServices
{
    // Enables C# 9 records and { get; init; } on net48, whose BCL predates this
    // marker type. Compiled only for the .NET Framework target.
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
