using PluginSdk.Commands;
using Sandbox.Game;
using VRage.Game;
using VRageMath;

namespace Magnetar.Legacy.Commands;

/// <summary>
/// Delivers command replies to chat through the game's scripted-message API.
/// Replies with an explicit colour use <c>SendChatMessageColored</c>; otherwise
/// the font name drives the rendered colour via <c>SendChatMessage</c>.
/// Non-broadcast replies target the caller's identity id; broadcasts use 0.
/// </summary>
public sealed class ServerCommandResponder : ICommandResponder
{
    public static readonly ServerCommandResponder Instance = new();

    public void Send(in CommandReply reply, in CommandCaller caller)
    {
        if (!reply.HasContent)
            return;

        long target = reply.Broadcast ? 0L : caller.IdentityId;
        string author = string.IsNullOrEmpty(reply.Author) ? "Server" : reply.Author;
        string font = string.IsNullOrEmpty(reply.Font) ? MyFontEnum.White : reply.Font;

        if (reply.Color.HasValue)
        {
            Color color = reply.Color.Value;
            MyVisualScriptLogicProvider.SendChatMessageColored(reply.Text, color, author, target, font);
        }
        else
        {
            MyVisualScriptLogicProvider.SendChatMessage(reply.Text, author, target, font);
        }
    }
}
