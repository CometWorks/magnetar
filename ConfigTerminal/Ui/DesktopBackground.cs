using Terminal.Gui;

namespace Magnetar.ConfigTerminal.Ui;

/// <summary>
/// The classic Turbo Vision blue desktop: fills its bounds with the ▒ shade
/// glyph. Sits behind all content windows.
/// </summary>
internal sealed class DesktopBackground : View
{
    public DesktopBackground()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
        CanFocus = false;
        ColorScheme = TurboVisionTheme.Desktop;
    }

    public override void Redraw(Rect bounds)
    {
        Driver.SetAttribute(ColorScheme.Normal);
        for (int y = 0; y < Bounds.Height; y++)
        {
            Move(0, y);
            for (int x = 0; x < Bounds.Width; x++)
                Driver.AddRune('▒'); // ▒
        }
    }
}
