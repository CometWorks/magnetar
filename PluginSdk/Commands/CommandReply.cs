using VRage.Game;
using VRageMath;

namespace PluginSdk.Commands
{
    /// <summary>
    /// A chat reply produced by a command handler. Carries the text plus the
    /// optional presentation metadata the Space Engineers chat supports: an
    /// arbitrary RGB <see cref="Color"/>, a <see cref="MyFontEnum"/> font name,
    /// the author label shown as the sender, and whether the message is sent to
    /// everyone or only to the caller.
    ///
    /// <para>
    /// Construct one with the factory helpers (<see cref="Ok"/>,
    /// <see cref="Info"/>, <see cref="Error"/>, <see cref="Announce"/>) and
    /// adjust it fluently with <see cref="WithColor"/>, <see cref="WithFont"/>,
    /// <see cref="WithAuthor"/> or <see cref="AsBroadcast"/>.
    /// </para>
    /// </summary>
    public readonly struct CommandReply
    {
        /// <summary>Reply text. An empty value sends nothing.</summary>
        public string Text { get; }

        /// <summary>Explicit RGB colour, or <c>null</c> to let the font decide.</summary>
        public Color? Color { get; }

        /// <summary>Font name, a <see cref="MyFontEnum"/> constant.</summary>
        public string Font { get; }

        /// <summary>Sender label, or <c>null</c> to use the command root title.</summary>
        public string Author { get; }

        /// <summary><c>true</c> to send to all players; otherwise only the caller.</summary>
        public bool Broadcast { get; }

        /// <summary><c>true</c> when there is text to send.</summary>
        public bool HasContent => !string.IsNullOrEmpty(Text);

        public CommandReply(
            string text,
            Color? color = null,
            string font = MyFontEnum.White,
            string author = null,
            bool broadcast = false
        )
        {
            Text = text;
            Color = color;
            Font = font;
            Author = author;
            Broadcast = broadcast;
        }

        /// <summary>An empty reply that sends nothing.</summary>
        public static CommandReply None => new CommandReply(null);

        /// <summary>A plain white reply to the caller.</summary>
        public static CommandReply Ok(string text) =>
            new CommandReply(text, font: MyFontEnum.White);

        /// <summary>An informational blue reply to the caller.</summary>
        public static CommandReply Info(string text) =>
            new CommandReply(text, font: MyFontEnum.Blue);

        /// <summary>An error reply to the caller, shown in red.</summary>
        public static CommandReply Error(string text) =>
            new CommandReply(text, font: MyFontEnum.Red);

        /// <summary>A reply broadcast to every player.</summary>
        public static CommandReply Announce(string text, Color? color = null) =>
            new CommandReply(text, color: color, font: MyFontEnum.White, broadcast: true);

        public CommandReply WithColor(Color color) =>
            new CommandReply(Text, color, Font, Author, Broadcast);

        public CommandReply WithFont(string font) =>
            new CommandReply(Text, Color, font, Author, Broadcast);

        public CommandReply WithAuthor(string author) =>
            new CommandReply(Text, Color, Font, author, Broadcast);

        public CommandReply AsBroadcast(bool broadcast = true) =>
            new CommandReply(Text, Color, Font, Author, broadcast);
    }
}
