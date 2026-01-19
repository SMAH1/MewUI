using Aprillz.MewUI.Controls;

namespace Aprillz.MewUI;

public static class MenuExtensions
{
    public static Menu ItemHeight(this Menu menu, double itemHeight)
    {
        menu.ItemHeight = itemHeight;
        return menu;
    }

    public static Menu ItemPadding(this Menu menu, Thickness itemPadding)
    {
        menu.ItemPadding = itemPadding;
        return menu;
    }

    public static MenuBar Spacing(this MenuBar bar, double spacing)
    {
        bar.Spacing = spacing;
        return bar;
    }

    public static MenuBar Items(this MenuBar bar, params MenuItem[] items)
    {
        bar.SetItems(items);
        return bar;
    }

    public static MenuBar Item(this MenuBar bar, MenuItem item)
    {
        bar.Add(item);
        return bar;
    }

    public static MenuBar Item(this MenuBar bar, string text, Menu menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        bar.Add(new MenuItem(text).Menu(menu));
        return bar;
    }

    public static MenuItem Text(this MenuItem item, string text)
    {
        item.Text = text ?? string.Empty;
        return item;
    }

    public static MenuItem Menu(this MenuItem item, Menu? menu)
    {
        item.SubMenu = menu;
        return item;
    }

    public static MenuItem Shortcut(this MenuItem item, string? shortcutText)
    {
        item.ShortcutText = shortcutText;
        return item;
    }
}
