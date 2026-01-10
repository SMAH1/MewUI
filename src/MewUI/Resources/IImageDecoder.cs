namespace Aprillz.MewUI.Resources;

public interface IImageDecoder
{
    ImageFormat Format { get; }

    bool TryDecode(ReadOnlySpan<byte> encoded, out DecodedBitmap bitmap);
}

