namespace Aprillz.MewUI.Platform.Linux;

internal sealed class NoClipboardService : IClipboardService
{
    public bool TrySetText(string text) => false;

    public bool TryGetText(out string text)
    {
        text = string.Empty;
        return false;
    }
}

