namespace Aprillz.MewUI;

/// <summary>
/// Image scaling / sampling quality hint for drawing bitmaps.
/// </summary>
public enum ImageInterpolationMode
{
    /// <summary>
    /// Backend default.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Nearest-neighbor sampling (pixelated, sharp).
    /// </summary>
    NearestNeighbor = 1,

    /// <summary>
    /// Linear sampling.
    /// </summary>
    Linear = 2,

    /// <summary>
    /// Best quality available (backend-dependent).
    /// </summary>
    HighQuality = 3,
}

