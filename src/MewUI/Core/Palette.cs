using Aprillz.MewUI.Primitives;

namespace Aprillz.MewUI.Core;

public sealed class Palette
{
    public string Name { get; }

    public Color WindowBackground { get; }
    public Color WindowText { get; }
    public Color ControlBackground { get; }
    public Color ControlBorder { get; }

    public Color ButtonFace { get; }
    public Color ButtonHoverBackground { get; }
    public Color ButtonPressedBackground { get; }
    public Color ButtonDisabledBackground { get; }

    public Color Accent { get; }
    public Color AccentText { get; }

    public Color SelectionBackground { get; }
    public Color SelectionText { get; }

    public Color DisabledText { get; }
    public Color PlaceholderText { get; }
    public Color TextBoxDisabledBackground { get; }
    public Color FocusRect { get; }

    public Color ScrollBarThumb { get; }
    public Color ScrollBarThumbHover { get; }
    public Color ScrollBarThumbActive { get; }

    public Palette(
        string name,
        Color windowBackground,
        Color windowText,
        Color controlBackground,
        Color buttonFace,
        Color buttonDisabledBackground,
        Color accent,
        Color? accentText = null)
    {
        Name = name;
        WindowBackground = windowBackground;
        WindowText = windowText;
        ControlBackground = controlBackground;
        ButtonFace = buttonFace;
        ButtonDisabledBackground = buttonDisabledBackground;

        Accent = accent;
        AccentText = accentText ?? GetDefaultAccentText(accent);

        var isDark = IsDarkBackground(windowBackground);
        var hoverT = isDark ? 0.22 : 0.14;
        var pressedT = isDark ? 0.32 : 0.24;

        SelectionBackground = ComputeSelectionBackground(controlBackground, accent);
        SelectionText = GetDefaultAccentText(SelectionBackground);

        ControlBorder = ComputeControlBorder(windowBackground, windowText, accent);
        DisabledText = ComputeDisabledText(windowBackground, windowText);
        PlaceholderText = DisabledText.WithAlpha(127);
        TextBoxDisabledBackground = ComputeTextBoxDisabledBackground(windowBackground, controlBackground);
        ButtonHoverBackground = buttonFace.Lerp(accent, hoverT);
        ButtonPressedBackground = buttonFace.Lerp(accent, pressedT);
        FocusRect = accent;

        (ScrollBarThumb, ScrollBarThumbHover, ScrollBarThumbActive) = ComputeScrollBarThumbs(windowBackground);
    }

    private Palette(
        string name,
        Color windowBackground,
        Color windowText,
        Color controlBackground,
        Color controlBorder,
        Color buttonFace,
        Color buttonHoverBackground,
        Color buttonPressedBackground,
        Color buttonDisabledBackground,
        Color accent,
        Color accentText,
        Color selectionBackground,
        Color selectionText,
        Color disabledText,
        Color placeholderText,
        Color textBoxDisabledBackground,
        Color focusRect,
        Color scrollBarThumb,
        Color scrollBarThumbHover,
        Color scrollBarThumbActive)
    {
        Name = name;
        WindowBackground = windowBackground;
        WindowText = windowText;
        ControlBackground = controlBackground;
        ControlBorder = controlBorder;
        ButtonFace = buttonFace;
        ButtonHoverBackground = buttonHoverBackground;
        ButtonPressedBackground = buttonPressedBackground;
        ButtonDisabledBackground = buttonDisabledBackground;
        Accent = accent;
        AccentText = accentText;
        SelectionBackground = selectionBackground;
        SelectionText = selectionText;
        DisabledText = disabledText;
        PlaceholderText = placeholderText;
        TextBoxDisabledBackground = textBoxDisabledBackground;
        FocusRect = focusRect;
        ScrollBarThumb = scrollBarThumb;
        ScrollBarThumbHover = scrollBarThumbHover;
        ScrollBarThumbActive = scrollBarThumbActive;
    }

    public Palette WithAccent(Color accent, Color? accentText = null)
    {
        var resolvedAccentText = accentText ?? GetDefaultAccentText(accent);
        var isDark = IsDarkBackground(WindowBackground);
        var hoverT = isDark ? 0.22 : 0.14;
        var pressedT = isDark ? 0.32 : 0.24;

        var controlBorder = ComputeControlBorder(WindowBackground, WindowText, accent);
        var disabledText = ComputeDisabledText(WindowBackground, WindowText);
        var placeholderText = disabledText;
        var textBoxDisabledBackground = ComputeTextBoxDisabledBackground(WindowBackground, ControlBackground);
        var selectionBackground = ComputeSelectionBackground(ControlBackground, accent);
        var selectionText = GetDefaultAccentText(selectionBackground);
        var (scrollBarThumb, scrollBarThumbHover, scrollBarThumbActive) = ComputeScrollBarThumbs(WindowBackground);

        return new Palette(
            name: Name,
            windowBackground: WindowBackground,
            windowText: WindowText,
            controlBackground: ControlBackground,
            controlBorder: controlBorder,
            buttonFace: ButtonFace,
            buttonHoverBackground: ButtonFace.Lerp(accent, hoverT),
            buttonPressedBackground: ButtonFace.Lerp(accent, pressedT),
            buttonDisabledBackground: ButtonDisabledBackground,
            accent: accent,
            accentText: resolvedAccentText,
            selectionBackground: selectionBackground,
            selectionText: selectionText,
            disabledText: disabledText,
            placeholderText: placeholderText,
            textBoxDisabledBackground: textBoxDisabledBackground,
            focusRect: accent,
            scrollBarThumb: scrollBarThumb,
            scrollBarThumbHover: scrollBarThumbHover,
            scrollBarThumbActive: scrollBarThumbActive);
    }

    private static bool IsDarkBackground(Color color) => (color.R + color.G + color.B) < 128 * 3;

    private static Color ComputeControlBorder(Color windowBackground, Color windowText, Color accent)
    {
        var isDark = IsDarkBackground(windowBackground);
        var baseBorder = windowBackground.Lerp(windowText, isDark ? 0.22 : 0.25);
        return baseBorder.Lerp(accent, isDark ? 0.10 : 0.08);
    }

    private static Color ComputeDisabledText(Color windowBackground, Color windowText)
    {
        var isDark = IsDarkBackground(windowBackground);
        return windowText.Lerp(windowBackground, isDark ? 0.32 : 0.58);
    }

    private static Color ComputeTextBoxDisabledBackground(Color windowBackground, Color controlBackground)
    {
        var isDark = IsDarkBackground(windowBackground);
        return controlBackground.Lerp(windowBackground, isDark ? 0.45 : 0.55);
    }

    private static Color ComputeSelectionBackground(Color controlBackground, Color accent)
    {
        var isDark = IsDarkBackground(controlBackground);
        var t = isDark ? 0.55 : 0.35;
        return controlBackground.Lerp(accent, t);
    }

    private static Color GetDefaultAccentText(Color accent)
    {
        var luma = (0.2126 * accent.R + 0.7152 * accent.G + 0.0722 * accent.B) / 255.0;
        return luma >= 0.6 ? Color.FromRgb(28, 28, 32) : Color.FromRgb(248, 246, 255);
    }

    private static (Color thumb, Color hover, Color active) ComputeScrollBarThumbs(Color windowBackground)
    {
        var isDark = IsDarkBackground(windowBackground);
        byte c = isDark ? (byte)255 : (byte)0;
        return (
            Color.FromArgb(0x44, c, c, c),
            Color.FromArgb(0x66, c, c, c),
            Color.FromArgb(0x88, c, c, c)
        );
    }
}
