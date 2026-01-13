using Aprillz.MewUI.Core;
using Aprillz.MewUI.Input;
using Aprillz.MewUI.Primitives;
using Aprillz.MewUI.Rendering;

using System.Buffers;

namespace Aprillz.MewUI.Controls;

/// <summary>
/// A single-line text input control.
/// </summary>
public class TextBox : TextBase
{
    protected override Color DefaultBackground => Theme.Current.Palette.ControlBackground;
    protected override Color DefaultBorderBrush => Theme.Current.Palette.ControlBorder;

    public TextBox()
    {
        BorderThickness = 1;
        Padding = new Thickness(4);
        MinHeight = Theme.Current.BaseControlHeight;
    }

    protected override void OnThemeChanged(Theme oldTheme, Theme newTheme)
    {
        base.OnThemeChanged(oldTheme, newTheme);

        if (MinHeight == oldTheme.BaseControlHeight)
        {
            MinHeight = newTheme.BaseControlHeight;
        }
    }

    protected override string NormalizePastedText(string text)
    {
        text ??= string.Empty;
        if (text.Length == 0)
        {
            return string.Empty;
        }

        // Single-line: preserve separation by converting newlines/tabs to spaces.
        if (text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
        {
            text = text.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        }

        if (!AcceptTab && text.IndexOf('\t') >= 0)
        {
            text = text.Replace('\t', ' ');
        }

        return text;
    }

    protected override Size MeasureContent(Size availableSize)
    {
        // Default size for empty text box
        var minHeight = FontSize + Padding.VerticalThickness + 4;
        return new Size(100, minHeight);
    }

    protected override void RenderTextContent(IGraphicsContext context, Rect contentBounds, IFont font, Theme theme, in VisualState state)
    {
        if (!Document.IsEmpty)
        {
            // Calculate text position with scroll offset
            var textX = contentBounds.X - HorizontalOffset;

            var text = Text.AsSpan();
            // Draw selection background if any
            if (HasSelection && IsFocused)
            {
                var (selStart, selEnd) = GetSelectionRange();


                var beforeSel = text[..selStart];
                var selection = text[selStart..selEnd];

                var beforeWidth = context.MeasureText(beforeSel, font).Width;
                var selWidth = context.MeasureText(selection, font).Width;

                var selRect = new Rect(textX + beforeWidth, contentBounds.Y,
                    selWidth, contentBounds.Height);
                context.FillRectangle(selRect, theme.Palette.SelectionBackground);
            }

            // Draw text
            var textColor = state.IsEnabled ? Foreground : theme.Palette.DisabledText;
            // Use backend vertical centering (font metrics differ from FontSize across renderers).
            context.DrawText(text, new Rect(textX, contentBounds.Y, 1_000_000, contentBounds.Height), font, textColor,
                TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
        }

        // Draw caret if focused
        if (state.IsFocused && !IsReadOnly)
        {
            var caretX = contentBounds.X - HorizontalOffset;
            if (CaretPosition > 0)
            {
                var textBefore = Text[..CaretPosition];
                caretX += context.MeasureText(textBefore, font).Width;
            }

            // Simple caret - could be animated
            context.DrawLine(
                new Point(caretX, contentBounds.Y + 2),
                new Point(caretX, contentBounds.Bottom - 2),
                theme.Palette.WindowText, 1);
        }
    }

    protected override void SetCaretFromPoint(Point point, Rect contentBounds)
    {
        var clickX = point.X - contentBounds.X + HorizontalOffset;
        CaretPosition = GetCharacterIndexFromX(clickX);
    }

    protected override void AutoScrollForSelectionDrag(Point point, Rect contentBounds)
    {
        // If the pointer goes beyond the text box, scroll in that direction.
        // This matches common native text box behavior and enables selecting off-screen text.
        const double edgeDip = 8;
        if (point.X < contentBounds.X + edgeDip)
        {
            SetHorizontalOffset(HorizontalOffset + point.X - (contentBounds.X + edgeDip), invalidateVisual: false);
        }
        else if (point.X > contentBounds.Right - edgeDip)
        {
            SetHorizontalOffset(HorizontalOffset + point.X - (contentBounds.Right - edgeDip), invalidateVisual: false);
        }

        ClampScrollOffset();
    }

    protected override void EnsureCaretVisibleCore(Rect contentBounds) => EnsureCaretVisible(contentBounds);

    private int GetCharacterIndexFromX(double x)
    {
        if (Document.IsEmpty)
        {
            return 0;
        }

        using var measure = BeginTextMeasurement();

        if (x <= 0)
        {
            return 0;
        }

        int length = Document.Length;
        char[]? rented = null;
        try
        {
            Span<char> buffer = length <= 0xFFFF
                ? stackalloc char[length]
                : (rented = ArrayPool<char>.Shared.Rent(length)).AsSpan(0, length);

            Document.CopyTo(buffer, 0, length);

            double totalWidth = measure.Context.MeasureText(buffer, measure.Font).Width;
            if (x >= totalWidth)
            {
                return length;
            }

            // Binary search to avoid O(n^2) measurement cost for long text.
            int lo = 0;
            int hi = length;
            while (lo < hi)
            {
                int mid = lo + ((hi - lo) / 2);
                double w = mid > 0 ? measure.Context.MeasureText(buffer[..mid], measure.Font).Width : 0;
                if (w < x)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }

            int idx = Math.Clamp(lo, 0, length);

            // Snap to the nearest caret position using midpoints for better feel.
            if (idx <= 0)
            {
                return 0;
            }

            double w0 = measure.Context.MeasureText(buffer[..(idx - 1)], measure.Font).Width;
            double w1 = measure.Context.MeasureText(buffer[..idx], measure.Font).Width;
            return x < (w0 + w1) / 2 ? idx - 1 : idx;
        }
        finally
        {
            if (rented != null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    private void EnsureCaretVisible(Rect contentBounds)
    {
        using var measure = BeginTextMeasurement();

        var text = Text.AsSpan();

        var caretX = CaretPosition > 0
            ? measure.Context.MeasureText(text[..CaretPosition], measure.Font).Width
            : 0;

        if (caretX - HorizontalOffset > contentBounds.Width - 5)
        {
            SetHorizontalOffset(caretX - contentBounds.Width + 10, invalidateVisual: false);
        }
        else if (caretX - HorizontalOffset < 5)
        {
            SetHorizontalOffset(Math.Max(0, caretX - 10), invalidateVisual: false);
        }

        ClampScrollOffset(measure.Context, measure.Font, contentBounds.Width);
    }

    private void ClampScrollOffset()
    {
        if (Document.IsEmpty)
        {
            SetHorizontalOffset(0, invalidateVisual: false);
            return;
        }

        var contentBounds = GetInteractionContentBounds();
        using var measure = BeginTextMeasurement();
        ClampScrollOffset(measure.Context, measure.Font, contentBounds.Width);
    }

    private void ClampScrollOffset(IGraphicsContext context, IFont font, double viewportWidthDip)
    {
        if (Document.IsEmpty)
        {
            SetHorizontalOffset(0, invalidateVisual: false);
            return;
        }

        double textWidth = context.MeasureText(Text, font).Width;
        double maxOffset = Math.Max(0, textWidth - Math.Max(0, viewportWidthDip));
        SetHorizontalOffset(Math.Clamp(HorizontalOffset, 0, maxOffset), invalidateVisual: false);
    }

    // Key handling is centralized in TextBase.
}
