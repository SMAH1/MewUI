using Aprillz.MewUI.Native.Com;
using Aprillz.MewUI.Native.Direct2D;
using Aprillz.MewUI.Resources;

namespace Aprillz.MewUI.Rendering.Direct2D;

internal sealed class Direct2DImage : IImage
{
    private const uint DXGI_FORMAT_B8G8R8A8_UNORM = 87;

    private readonly IPixelBufferSource _pixels;
    private int _pixelsVersion = -1;
    private byte[]? _premultiplied;
    private List<byte[]>? _mipBuffers;
    private nint _renderTarget;
    private int _renderTargetGeneration;
    private List<nint>? _mipBitmaps;
    private bool _disposed;

    public int PixelWidth { get; }
    public int PixelHeight { get; }

    public Direct2DImage(DecodedBitmap bmp)
    {
        if (bmp.PixelFormat != BitmapPixelFormat.Bgra32)
        {
            throw new NotSupportedException($"Unsupported pixel format: {bmp.PixelFormat}");
        }

        PixelWidth = bmp.WidthPx;
        PixelHeight = bmp.HeightPx;
        _pixels = new StaticPixelBufferSource(bmp.WidthPx, bmp.HeightPx, bmp.Data);
        _pixelsVersion = _pixels.Version;
    }

    public Direct2DImage(IPixelBufferSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.PixelFormat != BitmapPixelFormat.Bgra32)
        {
            throw new NotSupportedException($"Unsupported pixel format: {source.PixelFormat}");
        }

        PixelWidth = source.PixelWidth;
        PixelHeight = source.PixelHeight;
        _pixels = source;
        _pixelsVersion = source.Version;
    }

    public nint GetOrCreateBitmap(nint renderTarget, int renderTargetGeneration)
        => GetOrCreateBitmapForMip(renderTarget, renderTargetGeneration, mipLevel: 0);

    internal nint GetOrCreateBitmapForMip(nint renderTarget, int renderTargetGeneration, int mipLevel)
    {
        if (_disposed || renderTarget == 0)
        {
            return 0;
        }

        int v = _pixels.Version;
        if (_pixelsVersion != v)
        {
            _pixelsVersion = v;
            _premultiplied = null;
            _mipBuffers = null;

            ReleaseMipBitmaps();
            _renderTarget = 0;
            _renderTargetGeneration = 0;
        }

        if (_renderTarget != 0 && (_renderTarget != renderTarget || _renderTargetGeneration != renderTargetGeneration))
        {
            ReleaseMipBitmaps();
            _renderTarget = 0;
            _renderTargetGeneration = 0;
        }

        if (mipLevel < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mipLevel));
        }

        EnsureMipBuffers(mipLevel);
        if (_mipBuffers == null || _mipBuffers.Count <= mipLevel)
        {
            return 0;
        }

        EnsureMipBitmapsCapacity(mipLevel);
        if (_mipBitmaps != null && _mipBitmaps.Count > mipLevel)
        {
            nint existing = _mipBitmaps[mipLevel];
            if (existing != 0 && _renderTarget == renderTarget && _renderTargetGeneration == renderTargetGeneration)
            {
                return existing;
            }
        }

        if (_renderTarget == 0)
        {
            _renderTarget = renderTarget;
            _renderTargetGeneration = renderTargetGeneration;
        }

        var buffer = _mipBuffers[mipLevel];
        if (buffer.Length == 0)
        {
            return 0;
        }

        (int mipWidth, int mipHeight) = GetMipSize(mipLevel);

        var props = new D2D1_BITMAP_PROPERTIES(
            pixelFormat: new D2D1_PIXEL_FORMAT(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE.PREMULTIPLIED),
            dpiX: 96,
            dpiY: 96);

        nint bmpHandle = 0;
        unsafe
        {
            fixed (byte* p = buffer)
            {
                int hr = D2D1VTable.CreateBitmap(
                    (ID2D1RenderTarget*)renderTarget,
                    new D2D1_SIZE_U((uint)mipWidth, (uint)mipHeight),
                    srcData: (nint)p,
                    pitch: (uint)(mipWidth * 4),
                    props: props,
                    bitmap: out bmpHandle);

                if (hr < 0 || bmpHandle == 0)
                {
                    throw new InvalidOperationException($"ID2D1RenderTarget::CreateBitmap failed: 0x{hr:X8}");
                }
            }
        }

        if (_mipBitmaps != null)
        {
            _mipBitmaps[mipLevel] = bmpHandle;
        }
        return bmpHandle;
    }

    private (int width, int height) GetMipSize(int mipLevel)
    {
        int divisor = 1 << Math.Min(mipLevel, 30);
        int w = Math.Max(1, (PixelWidth + divisor - 1) / divisor);
        int h = Math.Max(1, (PixelHeight + divisor - 1) / divisor);
        return (w, h);
    }

    private void EnsureMipBuffers(int mipLevel)
    {
        if (_disposed)
        {
            return;
        }

        _mipBuffers ??= new List<byte[]>();
        if (_mipBuffers.Count == 0)
        {
            using var l = _pixels.Lock();
            if (l.Buffer.Length == 0)
            {
                _mipBuffers.Add(Array.Empty<byte>());
                return;
            }

            _premultiplied = PremultiplyIfNeeded(l.Buffer);
            _mipBuffers.Add(_premultiplied);
        }

        int srcW = PixelWidth;
        int srcH = PixelHeight;
        for (int i = 1; i <= mipLevel; i++)
        {
            if (_mipBuffers.Count > i)
            {
                srcW = Math.Max(1, (srcW + 1) / 2);
                srcH = Math.Max(1, (srcH + 1) / 2);
                continue;
            }

            var src = _mipBuffers[i - 1];
            var dst = Downsample2x(src, srcW, srcH, out int dstW, out int dstH);
            _mipBuffers.Add(dst);
            srcW = dstW;
            srcH = dstH;
        }
    }

    private static byte[] Downsample2x(byte[] src, int srcWidth, int srcHeight, out int dstWidth, out int dstHeight)
    {
        dstWidth = Math.Max(1, (srcWidth + 1) / 2);
        dstHeight = Math.Max(1, (srcHeight + 1) / 2);

        var dst = new byte[dstWidth * dstHeight * 4];

        for (int y = 0; y < dstHeight; y++)
        {
            int sy = y * 2;
            int sy1 = sy + 1;
            bool hasY1 = sy1 < srcHeight;

            for (int x = 0; x < dstWidth; x++)
            {
                int sx = x * 2;
                int sx1 = sx + 1;
                bool hasX1 = sx1 < srcWidth;

                int count = 1;
                int idx00 = (sy * srcWidth + sx) * 4;

                int b = src[idx00 + 0];
                int g = src[idx00 + 1];
                int r = src[idx00 + 2];
                int a = src[idx00 + 3];

                if (hasX1)
                {
                    count++;
                    int idx10 = (sy * srcWidth + sx1) * 4;
                    b += src[idx10 + 0];
                    g += src[idx10 + 1];
                    r += src[idx10 + 2];
                    a += src[idx10 + 3];
                }

                if (hasY1)
                {
                    count++;
                    int idx01 = (sy1 * srcWidth + sx) * 4;
                    b += src[idx01 + 0];
                    g += src[idx01 + 1];
                    r += src[idx01 + 2];
                    a += src[idx01 + 3];

                    if (hasX1)
                    {
                        count++;
                        int idx11 = (sy1 * srcWidth + sx1) * 4;
                        b += src[idx11 + 0];
                        g += src[idx11 + 1];
                        r += src[idx11 + 2];
                        a += src[idx11 + 3];
                    }
                }

                int di = (y * dstWidth + x) * 4;
                dst[di + 0] = (byte)((b + (count / 2)) / count);
                dst[di + 1] = (byte)((g + (count / 2)) / count);
                dst[di + 2] = (byte)((r + (count / 2)) / count);
                dst[di + 3] = (byte)((a + (count / 2)) / count);
            }
        }

        return dst;
    }

    private void EnsureMipBitmapsCapacity(int mipLevel)
    {
        _mipBitmaps ??= new List<nint>();
        while (_mipBitmaps.Count <= mipLevel)
        {
            _mipBitmaps.Add(0);
        }
    }

    private void ReleaseMipBitmaps()
    {
        if (_mipBitmaps == null)
        {
            return;
        }

        foreach (var bmp in _mipBitmaps)
        {
            ComHelpers.Release(bmp);
        }

        _mipBitmaps.Clear();
    }

    private static byte[] PremultiplyIfNeeded(byte[] bgra)
    {
        // Fast path: if no alpha < 255, return original buffer.
        for (int i = 3; i < bgra.Length; i += 4)
        {
            if (bgra[i] != 0xFF)
            {
                return Premultiply(bgra);
            }
        }
        return bgra;
    }

    private static byte[] Premultiply(ReadOnlySpan<byte> bgra)
    {
        var dst = new byte[bgra.Length];
        for (int i = 0; i < bgra.Length; i += 4)
        {
            byte b = bgra[i + 0];
            byte g = bgra[i + 1];
            byte r = bgra[i + 2];
            byte a = bgra[i + 3];

            uint alpha = a;
            dst[i + 3] = a;
            dst[i + 0] = (byte)((b * alpha + 127) / 255);
            dst[i + 1] = (byte)((g * alpha + 127) / 255);
            dst[i + 2] = (byte)((r * alpha + 127) / 255);
        }
        return dst;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        ReleaseMipBitmaps();
        _renderTarget = 0;
        _premultiplied = null;
        _mipBuffers = null;
    }
}
