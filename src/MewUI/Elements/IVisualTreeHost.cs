namespace Aprillz.MewUI.Elements;

internal interface IVisualTreeHost
{
    void VisitChildren(Action<Element> visitor);
}

