namespace Aprillz.MewUI;

public record class Theme
{
    private static readonly BuiltInAccentPair[] _builtInAccents =
    [
        new BuiltInAccentPair(Color.FromRgb(51, 122, 255),  Color.FromRgb(62, 141, 255)),  // Blue
        new BuiltInAccentPair(Color.FromRgb(143, 84, 219),  Color.FromRgb(158, 101, 232)), // Purple
        new BuiltInAccentPair(Color.FromRgb(236, 90, 161),  Color.FromRgb(244, 112, 174)), // Pink
        new BuiltInAccentPair(Color.FromRgb(236, 92, 86),   Color.FromRgb(244, 110, 104)), // Red
        new BuiltInAccentPair(Color.FromRgb(240, 140, 56),  Color.FromRgb(248, 156, 74)),  // Orange
        new BuiltInAccentPair(Color.FromRgb(245, 204, 67),  Color.FromRgb(250, 214, 90)),  // Yellow
        new BuiltInAccentPair(Color.FromRgb(132, 192, 79),  Color.FromRgb(150, 204, 98)),  // Green
        new BuiltInAccentPair(Color.FromRgb(150, 150, 150), Color.FromRgb(165, 165, 165)), // Gray
    ];

    public static Accent DefaultAccent { get; } = Accent.Blue;

    public static Theme Light => field ??= CreateLight();

    public static Theme Dark => field ??= CreateDark();

    private readonly struct BuiltInAccentPair
    {
        public Color Light { get; }

        public Color Dark { get; }

        public BuiltInAccentPair(Color light, Color dark)
        {
            Light = light;
            Dark = dark;
        }
    }

    public static IReadOnlyList<Accent> BuiltInAccents { get; } = Enum.GetValues<Accent>();

    public Color GetAccentColor(Accent accent) => GetAccentColor(accent, Palette.IsDarkBackground(Palette.WindowBackground));

    public static Color GetAccentColor(Accent accent, bool isDark)
    {
        int idx = (int)accent;
        if ((uint)idx >= (uint)_builtInAccents.Length)
        {
            idx = (int)Accent.Gray;
        }

        var pair = _builtInAccents[idx];
        return isDark ? pair.Dark : pair.Light;
    }

    public Theme WithAccent(Accent accent, Color? accentText = null)
        => WithAccent(GetAccentColor(accent), accentText);

    public static Theme Current
    {
        get => field ?? Light;
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (field == value)
            {
                return;
            }

            var old = Current;
            field = value;
            CurrentChanged?.Invoke(old, value);
        }
    }

    public static Action<Theme, Theme>? CurrentChanged { get; set; }

    public string Name { get; init; }

    public Palette Palette { get; }

    public double BaseControlHeight { get; init; }

    public double ControlCornerRadius { get; init; }

    public Thickness ListItemPadding { get; init; }

    public string FontFamily { get; init; }

    public double FontSize { get; init; }

    public FontWeight FontWeight { get; init; }

    // Scroll (thin style defaults)
    public double ScrollBarThickness { get; init; }

    public double ScrollBarHitThickness { get; init; }

    public double ScrollBarMinThumbLength { get; init; }

    public double ScrollWheelStep { get; init; }

    public double ScrollBarSmallChange { get; init; }

    public double ScrollBarLargeChange { get; init; }

    public Theme WithAccent(Color accent, Color? accentText = null)
    {
        return WithPalette(Palette.WithAccent(accent, accentText));
    }

    private Theme(
        string name,
        Palette palette,
        double baseControlHeight,
        double controlCornerRadius,
        Thickness listItemPadding,
        string fontFamily,
        double fontSize,
        FontWeight fontWeight,
        double scrollBarThickness,
        double scrollBarHitThickness,
        double scrollBarMinThumbLength,
        double scrollWheelStep,
        double scrollBarSmallChange,
        double scrollBarLargeChange)
    {
        Name = name;
        Palette = palette;
        BaseControlHeight = baseControlHeight;
        ControlCornerRadius = controlCornerRadius;
        ListItemPadding = listItemPadding;
        FontFamily = fontFamily;
        FontSize = fontSize;
        FontWeight = fontWeight;

        ScrollBarThickness = scrollBarThickness;
        ScrollBarHitThickness = scrollBarHitThickness;
        ScrollBarMinThumbLength = scrollBarMinThumbLength;
        ScrollWheelStep = scrollWheelStep;
        ScrollBarSmallChange = scrollBarSmallChange;
        ScrollBarLargeChange = scrollBarLargeChange;
    }

    public Theme WithPalette(Palette palette)
    {
        ArgumentNullException.ThrowIfNull(palette);

        return new Theme(
            Name,
            palette,
            BaseControlHeight,
            ControlCornerRadius,
            ListItemPadding,
            FontFamily,
            FontSize,
            FontWeight,
            ScrollBarThickness,
            ScrollBarHitThickness,
            ScrollBarMinThumbLength,
            ScrollWheelStep,
            ScrollBarSmallChange,
            ScrollBarLargeChange);
    }

    private static Theme CreateLight()
    {
        var accent = DefaultAccent;
        var palette = new Palette(
            name: "Light",
            baseColors: new ThemeSeed(
                WindowBackground: Color.FromRgb(250, 250, 250),
                WindowText: Color.FromRgb(30, 30, 30),
                ControlBackground: Color.White,
                ButtonFace: Color.FromRgb(232, 232, 232),
                ButtonDisabledBackground: Color.FromRgb(204, 204, 204)),
            accent: GetAccentColor(accent, isDark: false));

        return new Theme(
            name: "Light",
            palette: palette,
            baseControlHeight: 28,
            controlCornerRadius: 4,
            listItemPadding: new Thickness(8, 2, 8, 2),
            fontFamily: "Segoe UI",
            fontSize: 12,
            fontWeight: FontWeight.Normal,
            scrollBarThickness: 4,
            scrollBarHitThickness: 10,
            scrollBarMinThumbLength: 14,
            scrollWheelStep: 32,
            scrollBarSmallChange: 24,
            scrollBarLargeChange: 120);
    }

    private static Theme CreateDark()
    {
        var accent = DefaultAccent;
        var palette = new Palette(
            name: "Dark",
            baseColors: new ThemeSeed(
                WindowBackground: Color.FromRgb(30, 30, 30),
                WindowText: Color.FromRgb(230, 230, 232),
                ControlBackground: Color.FromRgb(26, 26, 27),
                ButtonFace: Color.FromRgb(48, 48, 50),
                ButtonDisabledBackground: Color.FromRgb(60, 60, 64)),
            accent: GetAccentColor(accent, isDark: true));

        return new Theme(
            name: "Dark",
            palette: palette,
            baseControlHeight: 28,
            controlCornerRadius: 4,
            listItemPadding: new Thickness(8, 2, 8, 2),
            fontFamily: "Segoe UI",
            fontSize: 12,
            fontWeight: FontWeight.Normal,
            scrollBarThickness: 4,
            scrollBarHitThickness: 10,
            scrollBarMinThumbLength: 14,
            scrollWheelStep: 32,
            scrollBarSmallChange: 24,
            scrollBarLargeChange: 120);
    }
}

public enum Accent
{
    Blue,
    Purple,
    Pink,
    Red,
    Orange,
    Yellow,
    Green,
    Gray,
}