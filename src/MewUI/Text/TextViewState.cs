namespace Aprillz.MewUI.Text;

internal sealed class TextViewState
{
    public double HorizontalOffset { get; private set; }
    public double VerticalOffset { get; private set; }

    public bool SetHorizontalOffset(double value)
    {
        value = SanitizeOffset(value);
        if (HorizontalOffset == value)
        {
            return false;
        }

        HorizontalOffset = value;
        return true;
    }

    public bool SetVerticalOffset(double value)
    {
        value = SanitizeOffset(value);
        if (VerticalOffset == value)
        {
            return false;
        }

        VerticalOffset = value;
        return true;
    }

    public bool SetScrollOffsets(double horizontal, double vertical)
    {
        bool changedH = SetHorizontalOffset(horizontal);
        bool changedV = SetVerticalOffset(vertical);
        return changedH || changedV;
    }

    private static double SanitizeOffset(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0;
        }

        return value;
    }
}

