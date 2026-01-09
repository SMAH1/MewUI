using Aprillz.MewUI.Core;
using Aprillz.MewUI.Binding;
using Aprillz.MewUI.Elements;
using Aprillz.MewUI.Input;
using Aprillz.MewUI.Primitives;
using Aprillz.MewUI.Rendering;

namespace Aprillz.MewUI.Controls;

public class ListBox : Control
{
    private readonly List<string> _items = new();
    private ValueBinding<int>? _selectedIndexBinding;
    private bool _updatingFromSource;
    private readonly ScrollBar _vBar;
    private double _verticalOffset;
    private double _extentHeight;
    private double _viewportHeight;

    public IList<string> Items => _items;

    public int SelectedIndex
    {
        get;
        set
        {
            int clamped = value;
            if (_items.Count == 0)
                clamped = -1;
            else
                clamped = Math.Clamp(value, -1, _items.Count - 1);

            if (field == clamped)
                return;

            field = clamped;
            SelectionChanged?.Invoke(field);
            InvalidateVisual();
        }
    } = -1;

    public string? SelectedItem => SelectedIndex >= 0 && SelectedIndex < _items.Count ? _items[SelectedIndex] : null;

    public double ItemHeight
    {
        get;
        set { field = value; InvalidateMeasure(); }
    } = double.NaN;

    public Action<int>? SelectionChanged { get; set; }

    public override bool Focusable => true;

    protected override Color DefaultBackground => Theme.Current.ControlBackground;
    protected override Color DefaultBorderBrush => Theme.Current.ControlBorder;

    public ListBox()
    {
        BorderThickness = 1;
        Padding = new Thickness(1);

        _vBar = new ScrollBar { Orientation = Panels.Orientation.Vertical, IsVisible = false };
        _vBar.Parent = this;
        _vBar.ValueChanged = v =>
        {
            _verticalOffset = ClampVerticalOffset(v);
            InvalidateVisual();
        };
    }

    protected override Size MeasureContent(Size availableSize)
    {
        using var measure = BeginTextMeasurement();

        double maxWidth = 0;
        foreach (var item in _items)
        {
            if (string.IsNullOrEmpty(item))
                continue;
            maxWidth = Math.Max(maxWidth, measure.Context.MeasureText(item, measure.Font).Width);
        }

        double itemHeight = ResolveItemHeight();
        double height = _items.Count * itemHeight;

        // Cache extent/viewport for scroll bar (viewport is approximated here; final value computed in Arrange).
        _extentHeight = height;
        _viewportHeight = double.IsPositiveInfinity(availableSize.Height) ? height : Math.Max(0, availableSize.Height - Padding.VerticalThickness);

        double desiredHeight = double.IsPositiveInfinity(availableSize.Height)
            ? height
            : Math.Min(height, _viewportHeight);

        return new Size(maxWidth, desiredHeight).Inflate(Padding);
    }

    protected override void ArrangeContent(Rect bounds)
    {
        base.ArrangeContent(bounds);

        var theme = GetTheme();
        var snapped = GetSnappedBorderBounds(Bounds);
        var borderInset = GetBorderVisualInset();
        var contentBounds = snapped.Deflate(Padding).Deflate(new Thickness(borderInset));

        _viewportHeight = Math.Max(0, contentBounds.Height);
        _verticalOffset = ClampVerticalOffset(_verticalOffset);

        bool needV = _extentHeight > _viewportHeight + 0.5;
        _vBar.IsVisible = needV;

        if (_vBar.IsVisible)
        {
            double t = theme.ScrollBarThickness;
            const double inset = 1;

            _vBar.Minimum = 0;
            _vBar.Maximum = Math.Max(0, _extentHeight - _viewportHeight);
            _vBar.ViewportSize = _viewportHeight;
            _vBar.SmallChange = theme.ScrollBarSmallChange;
            _vBar.LargeChange = theme.ScrollBarLargeChange;
            _vBar.Value = _verticalOffset;

            _vBar.Arrange(new Rect(
                contentBounds.Right - t - inset,
                contentBounds.Y + inset,
                t,
                Math.Max(0, contentBounds.Height - inset * 2)));
        }
    }

    protected override void OnRender(IGraphicsContext context)
    {
        var theme = GetTheme();
        var bounds = GetSnappedBorderBounds(Bounds);
        double radius = theme.ControlCornerRadius;
        var borderInset = GetBorderVisualInset();
        double itemRadius = Math.Max(0, radius - borderInset);

        var bg = IsEnabled ? Background : theme.TextBoxDisabledBackground;
        var borderColor = BorderBrush;
        if (IsEnabled)
        {
            if (IsFocused)
                borderColor = theme.Accent;
            else if (IsMouseOver)
                borderColor = BorderBrush.Lerp(theme.Accent, 0.6);
        }
        DrawBackgroundAndBorder(context, bounds, bg, borderColor, radius);

        if (_items.Count == 0)
            return;

        var contentBounds = bounds.Deflate(Padding).Deflate(new Thickness(borderInset));
        context.Save();
        context.SetClip(contentBounds);

        var font = GetFont();
        double itemHeight = ResolveItemHeight();

        int first = itemHeight <= 0 ? 0 : Math.Max(0, (int)Math.Floor(_verticalOffset / itemHeight));
        double offsetInItem = itemHeight <= 0 ? 0 : _verticalOffset - first * itemHeight;
        double yStart = contentBounds.Y - offsetInItem;
        int visibleCount = itemHeight <= 0 ? _items.Count : (int)Math.Ceiling((contentBounds.Height + offsetInItem) / itemHeight) + 1;
        int lastExclusive = Math.Min(_items.Count, first + Math.Max(0, visibleCount));

        for (int i = first; i < lastExclusive; i++)
        {
            double y = yStart + (i - first) * itemHeight;
            var itemRect = new Rect(contentBounds.X, y, contentBounds.Width, itemHeight);

            bool selected = i == SelectedIndex;
            if (selected)
            {
                var selectionBg = theme.SelectionBackground;
                if (itemRadius > 0)
                    context.FillRoundedRectangle(itemRect, itemRadius, itemRadius, selectionBg);
                else
                    context.FillRectangle(itemRect, selectionBg);
            }

            var textColor = selected ? theme.SelectionText : (IsEnabled ? Foreground : theme.DisabledText);
            context.DrawText(_items[i] ?? string.Empty, itemRect.Deflate(new Thickness(2, 0, 2, 0)), font, textColor,
                TextAlignment.Left, TextAlignment.Center, TextWrapping.NoWrap);
        }

        context.Restore();

        if (_vBar.IsVisible)
            _vBar.Render(context);
    }

    public override UIElement? HitTest(Point point)
    {
        if (!IsVisible || !IsHitTestVisible || !IsEnabled)
            return null;

        if (_vBar.IsVisible && _vBar.Bounds.Contains(point))
            return _vBar;

        return base.HitTest(point);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (!IsEnabled || e.Button != MouseButton.Left)
            return;

        Focus();

        var contentBounds = GetSnappedBorderBounds(Bounds)
            .Deflate(Padding)
            .Deflate(new Thickness(GetBorderVisualInset()));
        int index = (int)((e.Position.Y - contentBounds.Y + _verticalOffset) / ResolveItemHeight());
        if (index >= 0 && index < _items.Count)
        {
            SelectedIndex = index;
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (e.Handled || !_vBar.IsVisible)
            return;

        int notches = Math.Sign(e.Delta);
        if (notches == 0)
            return;

        _verticalOffset = ClampVerticalOffset(_verticalOffset - notches * GetTheme().ScrollWheelStep);
        _vBar.Value = _verticalOffset;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (!IsEnabled)
            return;

        if (e.Key == Input.Key.Up)
        {
            if (_items.Count > 0)
                SelectedIndex = Math.Max(0, SelectedIndex <= 0 ? 0 : SelectedIndex - 1);
            e.Handled = true;
        }
        else if (e.Key == Input.Key.Down)
        {
            if (_items.Count > 0)
                SelectedIndex = Math.Min(_items.Count - 1, SelectedIndex < 0 ? 0 : SelectedIndex + 1);
            e.Handled = true;
        }
    }

    public void AddItem(string item)
    {
        _items.Add(item ?? string.Empty);
        InvalidateMeasure();
        InvalidateVisual();
    }

    public void ClearItems()
    {
        _items.Clear();
        SelectedIndex = -1;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private double ResolveItemHeight()
    {
        if (!double.IsNaN(ItemHeight) && ItemHeight > 0)
            return ItemHeight;
        return Math.Max(18, FontSize * 2);
    }

    private double ClampVerticalOffset(double value)
    {
        double max = Math.Max(0, _extentHeight - _viewportHeight);
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0;
        return Math.Clamp(value, 0, max);
    }

    public void SetSelectedIndexBinding(
        Func<int> get,
        Action<int> set,
        Action<Action>? subscribe = null,
        Action<Action>? unsubscribe = null)
    {
        if (get == null) throw new ArgumentNullException(nameof(get));
        if (set == null) throw new ArgumentNullException(nameof(set));

        _selectedIndexBinding?.Dispose();
        _selectedIndexBinding = new ValueBinding<int>(
            get,
            set,
            subscribe,
            unsubscribe,
            onSourceChanged: () =>
            {
                _updatingFromSource = true;
                try { SelectedIndex = get(); }
                finally { _updatingFromSource = false; }
            });

        var existing = SelectionChanged;
        SelectionChanged = i =>
        {
            existing?.Invoke(i);

            if (_updatingFromSource)
                return;

            _selectedIndexBinding?.Set(i);
        };

        _updatingFromSource = true;
        try { SelectedIndex = get(); }
        finally { _updatingFromSource = false; }
    }

    protected override void OnDispose()
    {
        _selectedIndexBinding?.Dispose();
        _selectedIndexBinding = null;
    }
}
