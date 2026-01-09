namespace Aprillz.MewUI.Platform;

public interface IClipboardService
{
    bool TrySetText(string text);

    bool TryGetText(out string text);
}

