using Aprillz.MewUI.Rendering;

namespace Aprillz.MewUI.Controls;

public sealed class ToolTip : Control
{
    public string Text
    {
        get;
        set
        {
            value ??= string.Empty;
            if (field == value)
            {
                return;
            }

            field = value;
            InvalidateMeasure();
            InvalidateVisual();
        }
    } = string.Empty;

    public ToolTip()
    {
        Padding = new Thickness(8, 4, 8, 4);
        BorderThickness = 1;
    }

    protected override Color DefaultBackground => Theme.Current.Palette.ControlBackground;

    protected override Color DefaultBorderBrush => Theme.Current.Palette.ControlBorder;

    protected override Size MeasureContent(Size availableSize)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return Padding.HorizontalThickness > 0 || Padding.VerticalThickness > 0
                ? new Size(Padding.HorizontalThickness, Padding.VerticalThickness)
                : Size.Empty;
        }

        using var measure = BeginTextMeasurement();
        var textSize = measure.Context.MeasureText(Text, measure.Font);
        return textSize.Inflate(Padding);
    }

    protected override void OnRender(IGraphicsContext context)
    {
        var theme = GetTheme();
        var bounds = GetSnappedBorderBounds(Bounds);
        var dpiScale = GetDpi() / 96.0;
        double radius = LayoutRounding.RoundToPixel(theme.ControlCornerRadius, dpiScale);

        DrawBackgroundAndBorder(context, bounds, Background, BorderBrush, radius);

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }

        var borderInset = GetBorderVisualInset();
        var contentBounds = bounds.Deflate(new Thickness(borderInset)).Deflate(Padding);
        context.DrawText(Text, contentBounds, GetFont(), Foreground,
            TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
    }
}

