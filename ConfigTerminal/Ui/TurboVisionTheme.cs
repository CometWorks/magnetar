using Terminal.Gui;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// Classic 16-color Turbo Vision / Turbo Pascal 7 IDE palette expressed as
/// Terminal.Gui v1 ColorSchemes. Every colour is within the CGA 16-color set so
/// it renders identically under the curses, Windows and Net drivers.
/// </summary>
internal static class TurboVisionTheme
{
    public static ColorScheme Window { get; private set; }
    public static ColorScheme Menu { get; private set; }
    public static ColorScheme Dialog { get; private set; }
    public static ColorScheme Error { get; private set; }
    public static ColorScheme Desktop { get; private set; }

    private static Terminal.Gui.Attribute A(Color fg, Color bg) => Terminal.Gui.Attribute.Make(fg, bg);

    public static void Apply()
    {
        Window = new ColorScheme
        {
            Normal = A(Color.White, Color.Blue),
            Focus = A(Color.Black, Color.Cyan),
            HotNormal = A(Color.BrightYellow, Color.Blue),
            HotFocus = A(Color.BrightYellow, Color.Cyan),
            Disabled = A(Color.Gray, Color.Blue),
        };

        Menu = new ColorScheme
        {
            Normal = A(Color.Black, Color.Gray),
            Focus = A(Color.White, Color.Green),
            HotNormal = A(Color.Red, Color.Gray),
            HotFocus = A(Color.BrightYellow, Color.Green),
            Disabled = A(Color.DarkGray, Color.Gray),
        };

        Dialog = new ColorScheme
        {
            Normal = A(Color.Black, Color.Gray),
            Focus = A(Color.Black, Color.Cyan),
            HotNormal = A(Color.Blue, Color.Gray),
            HotFocus = A(Color.Blue, Color.Cyan),
            Disabled = A(Color.DarkGray, Color.Gray),
        };

        Error = new ColorScheme
        {
            Normal = A(Color.White, Color.Red),
            Focus = A(Color.Black, Color.Gray),
            HotNormal = A(Color.BrightYellow, Color.Red),
            HotFocus = A(Color.BrightYellow, Color.Gray),
            Disabled = A(Color.Gray, Color.Red),
        };

        Desktop = new ColorScheme
        {
            Normal = A(Color.Gray, Color.Blue),
            Focus = A(Color.Gray, Color.Blue),
            HotNormal = A(Color.Gray, Color.Blue),
            HotFocus = A(Color.Gray, Color.Blue),
            Disabled = A(Color.Gray, Color.Blue),
        };

        Colors.Base = Window;
        Colors.Menu = Menu;
        Colors.Dialog = Dialog;
        Colors.Error = Error;
        Colors.TopLevel = Desktop;
    }
}
