namespace Aprillz.MewUI.Controls;

public abstract class MenuEntry
{
    internal MenuEntry() { }
}

public sealed class MenuSeparator : MenuEntry
{
    public static readonly MenuSeparator Instance = new();

    private MenuSeparator() { }

    internal static double MenuSeparatorHeight => 3;
}

public sealed class MenuItem : MenuEntry
{
    public MenuItem() { }

    public MenuItem(string text) => Text = text ?? string.Empty;

    public string Text { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public string? ShortcutText { get; set; }

    public Action? Click { get; set; }

    public Menu? SubMenu { get; set; }

    public override string ToString() => Text;
}

public sealed class Menu
{
    private readonly List<MenuEntry> _items = new();

    public IList<MenuEntry> Items => _items;

    /// <summary>
    /// Optional per-menu item height override (in DIP). When NaN, the visual presenter chooses a theme-based default.
    /// </summary>
    public double ItemHeight { get; set; } = double.NaN;

    /// <summary>
    /// Optional per-menu item padding override. When null, the visual presenter chooses a theme-based default.
    /// </summary>
    public Thickness? ItemPadding { get; set; }

    public Menu Add(MenuEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _items.Add(entry);
        return this;
    }

    public Menu Item(string text, Action? onClick = null, bool isEnabled = true, string? shortcutText = null)
    {
        _items.Add(new MenuItem
        {
            Text = text ?? string.Empty,
            Click = onClick,
            IsEnabled = isEnabled,
            ShortcutText = shortcutText
        });
        return this;
    }

    public Menu SubMenu(string text, Menu subMenu, bool isEnabled = true, string? shortcutText = null)
    {
        ArgumentNullException.ThrowIfNull(subMenu);

        _items.Add(new MenuItem
        {
            Text = text ?? string.Empty,
            IsEnabled = isEnabled,
            ShortcutText = shortcutText,
            SubMenu = subMenu
        });
        return this;
    }

    public Menu Separator()
    {
        _items.Add(MenuSeparator.Instance);
        return this;
    }
}
