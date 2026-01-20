namespace Aprillz.MewUI.Platform;

public interface IClipboardService
{
    bool TrySetText(string text);

    bool TryGetText(out string text);
}

internal sealed class NoClipboardService : IClipboardService
{
    public bool TrySetText(string text) => false;

    public bool TryGetText(out string text)
    {
        text = string.Empty;
        return false;
    }
}

