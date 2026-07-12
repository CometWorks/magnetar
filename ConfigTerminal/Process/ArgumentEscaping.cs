#if NETFRAMEWORK
using System.Collections.Generic;
using System.Text;

namespace Magnetar.ConfigTerminal.Process;

/// <summary>
/// Windows command-line argument quoting for net48, which lacks
/// <see cref="System.Diagnostics.ProcessStartInfo.ArgumentList"/>. Mirrors the
/// algorithm the modern runtime uses to build the <c>Arguments</c> string so each
/// element survives a <c>CommandLineToArgvW</c> round-trip (quotes, spaces and
/// trailing backslashes are escaped correctly).
/// </summary>
internal static class ArgumentEscaping
{
    public static string Join(IEnumerable<string> arguments)
    {
        var sb = new StringBuilder();
        foreach (string arg in arguments)
        {
            if (sb.Length > 0)
                sb.Append(' ');
            AppendArgument(sb, arg);
        }
        return sb.ToString();
    }

    private static void AppendArgument(StringBuilder sb, string argument)
    {
        if (argument.Length != 0 && ContainsNoWhitespaceOrQuotes(argument))
        {
            sb.Append(argument);
            return;
        }

        sb.Append('"');
        int idx = 0;
        while (idx < argument.Length)
        {
            char c = argument[idx++];
            if (c == '\\')
            {
                int backslashes = 1;
                while (idx < argument.Length && argument[idx] == '\\')
                {
                    idx++;
                    backslashes++;
                }

                if (idx == argument.Length)
                    sb.Append('\\', backslashes * 2); // escape trailing backslashes before the closing quote
                else if (argument[idx] == '"')
                {
                    sb.Append('\\', backslashes * 2 + 1); // escape backslashes and the quote
                    sb.Append('"');
                    idx++;
                }
                else
                    sb.Append('\\', backslashes); // backslashes not before a quote are literal
            }
            else if (c == '"')
            {
                sb.Append('\\');
                sb.Append('"');
            }
            else
            {
                sb.Append(c);
            }
        }
        sb.Append('"');
    }

    private static bool ContainsNoWhitespaceOrQuotes(string s)
    {
        foreach (char c in s)
            if (char.IsWhiteSpace(c) || c == '"')
                return false;
        return true;
    }
}
#endif
