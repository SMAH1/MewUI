namespace Aprillz.MewUI.Resources;

public enum BitmapPixelFormat
{
    Bgra32 = 0,
}

public readonly record struct DecodedBitmap(
    int WidthPx,
    int HeightPx,
    BitmapPixelFormat PixelFormat,
    byte[] Data)
{
    public int StrideBytes => WidthPx * 4;
}

