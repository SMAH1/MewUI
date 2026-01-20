using System.Runtime.CompilerServices;

namespace Aprillz.MewUI.Rendering.Gdi.Simd;

/// <summary>
/// Dispatches SIMD operations to the best available implementation.
/// Automatically selects AVX2, SSE2, or scalar based on CPU capabilities.
/// </summary>
internal static class GdiSimdDispatcher
{
    /// <summary>
    /// Builds a 256-entry premultiplied BGRA lookup table for the given source color.
    /// Index by alpha (0..255) and store directly into a 32bpp BGRA buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void BuildPremultipliedBgraTable(Span<uint> table, byte srcB, byte srcG, byte srcR)
    {
        if (table.Length < 256)
        {
            throw new ArgumentException("Table must have length >= 256.", nameof(table));
        }

        table[0] = 0;
        for (int a = 1; a < 256; a++)
        {
            byte alpha = (byte)a;
            byte pb = Premultiply8(srcB, alpha);
            byte pg = Premultiply8(srcG, alpha);
            byte pr = Premultiply8(srcR, alpha);
            table[a] = (uint)(pb | (pg << 8) | (pr << 16) | (alpha << 24));
        }
    }

    /// <summary>
    /// Writes a row of premultiplied BGRA pixels using a prebuilt alpha table.
    /// This avoids per-pixel premultiply math in tight loops.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void WritePremultipliedBgraRow(byte* dstBgra, ReadOnlySpan<byte> alphaRow, ReadOnlySpan<uint> table)
    {
        if (dstBgra == null || alphaRow.Length == 0)
        {
            return;
        }

        if (table.Length < 256)
        {
            throw new ArgumentException("Table must have length >= 256.", nameof(table));
        }

        uint* dst = (uint*)dstBgra;
        int count = alphaRow.Length;

        fixed (byte* aPtr = alphaRow)
        fixed (uint* tablePtr = table)
        {
            int i = 0;
            for (; i + 4 <= count; i += 4)
            {
                dst[i + 0] = tablePtr[aPtr[i + 0]];
                dst[i + 1] = tablePtr[aPtr[i + 1]];
                dst[i + 2] = tablePtr[aPtr[i + 2]];
                dst[i + 3] = tablePtr[aPtr[i + 3]];
            }

            for (; i < count; i++)
            {
                dst[i] = tablePtr[aPtr[i]];
            }
        }
    }

    /// <summary>
    /// Writes a row of premultiplied BGRA pixels from alpha values.
    /// Automatically uses the best available SIMD implementation.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void WritePremultipliedBgraRow(
        byte* dstBgra,
        ReadOnlySpan<byte> alphaRow,
        byte srcB,
        byte srcG,
        byte srcR)
    {
        if (dstBgra == null || alphaRow.Length == 0)
        {
            return;
        }

        if (SimdCapabilities.HasAvx2)
        {
            Avx2Processor.WritePremultipliedBgraRow(dstBgra, alphaRow, srcB, srcG, srcR);
        }
        else if (SimdCapabilities.HasSse2)
        {
            Sse2Processor.WritePremultipliedBgraRow(dstBgra, alphaRow, srcB, srcG, srcR);
        }
        else
        {
            WritePremultipliedBgraRowScalar(dstBgra, alphaRow, srcB, srcG, srcR);
        }
    }

    /// <summary>
    /// Fills a row of BGRA pixels with a solid premultiplied color.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void FillBgraRow(byte* dst, int count, byte b, byte g, byte r, byte a)
    {
        if (dst == null || count <= 0)
        {
            return;
        }

        if (SimdCapabilities.HasAvx2)
        {
            Avx2Processor.FillBgraRow(dst, count, b, g, r, a);
        }
        else if (SimdCapabilities.HasSse2)
        {
            Sse2Processor.FillBgraRow(dst, count, b, g, r, a);
        }
        else
        {
            FillBgraRowScalar(dst, count, b, g, r, a);
        }
    }

    /// <summary>
    /// Clears a row of pixels to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ClearRow(byte* dst, int byteCount)
    {
        if (dst == null || byteCount <= 0)
        {
            return;
        }

        if (SimdCapabilities.HasAvx2)
        {
            Avx2Processor.ClearRow(dst, byteCount);
        }
        else if (SimdCapabilities.HasSse2)
        {
            Sse2Processor.ClearRow(dst, byteCount);
        }
        else
        {
            new Span<byte>(dst, byteCount).Clear();
        }
    }

    /// <summary>
    /// Clears an entire 2D region efficiently.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe void ClearRegion(byte* basePtr, int stride, int width, int height)
    {
        if (basePtr == null || width <= 0 || height <= 0)
        {
            return;
        }

        int rowBytes = width * 4;

        for (int y = 0; y < height; y++)
        {
            ClearRow(basePtr + y * stride, rowBytes);
        }
    }

    #region Scalar Fallbacks

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void WritePremultipliedBgraRowScalar(
        byte* dstBgra,
        ReadOnlySpan<byte> alphaRow,
        byte srcB,
        byte srcG,
        byte srcR)
    {
        int width = alphaRow.Length;
        byte* p = dstBgra;

        for (int i = 0; i < width; i++)
        {
            byte a = alphaRow[i];
            if (a == 0)
            {
                p[0] = 0;
                p[1] = 0;
                p[2] = 0;
                p[3] = 0;
            }
            else
            {
                p[0] = Premultiply8(srcB, a);
                p[1] = Premultiply8(srcG, a);
                p[2] = Premultiply8(srcR, a);
                p[3] = a;
            }
            p += 4;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe void FillBgraRowScalar(byte* dst, int count, byte b, byte g, byte r, byte a)
    {
        byte pb = Premultiply8(b, a);
        byte pg = Premultiply8(g, a);
        byte pr = Premultiply8(r, a);

        uint pixel = (uint)(pb | (pg << 8) | (pr << 16) | (a << 24));
        uint* p = (uint*)dst;

        for (int i = 0; i < count; i++)
        {
            *p++ = pixel;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Premultiply8(byte c, byte a)
    {
        int t = c * a + 128;
        t += t >> 8;
        return (byte)(t >> 8);
    }

    #endregion
}
