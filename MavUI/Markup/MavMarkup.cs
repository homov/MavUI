using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace Mav.Markup;

/// <summary>
/// Hand-written fluent helpers of the C# markup layer. Setters for each control's properties and events are generated
/// (see <c>tools/MarkupGenerator</c>); this class holds the helpers that are not tied to one control.
/// Methods use a lower-case first letter, for example <c>new MavButton().text("Save").width(120)</c>.
/// </summary>
public static class MavMarkup
{
    /// <summary>Sets any styled property.</summary>
    public static TControl property<TControl, TValue>(this TControl control, StyledProperty<TValue> property, TValue value)
        where TControl : AvaloniaObject
    {
        control.SetValue(property, value);
        return control;
    }

    /// <summary>Binds a property to a binding, for example <c>new Binding("Title")</c>.</summary>
    public static TControl binding<TControl>(this TControl control, AvaloniaProperty property, IBinding binding)
        where TControl : AvaloniaObject
    {
        control.Bind(property, binding);
        return control;
    }

    /// <summary>Sets the name of the element.</summary>
    public static TControl name<TControl>(this TControl control, string name)
        where TControl : StyledElement
    {
        control.Name = name;
        return control;
    }

    /// <summary>Adds children to a panel, in the given order.</summary>
    public static TPanel children<TPanel>(this TPanel panel, params Control[] children)
        where TPanel : Panel
    {
        foreach (var child in children)
            panel.Children.Add(child);
        return panel;
    }

    /// <summary>Adds items to an items control. Do not combine with an items source.</summary>
    public static TControl items<TControl>(this TControl control, IEnumerable items)
        where TControl : ItemsControl
    {
        foreach (var item in items)
            control.Items.Add(item);
        return control;
    }

    /// <summary>Sets the content of a content control.</summary>
    public static TControl content<TControl>(this TControl control, object? content)
        where TControl : ContentControl
    {
        control.Content = content;
        return control;
    }

    /// <summary>Sets the child of a decorator such as a border.</summary>
    public static TDecorator child<TDecorator>(this TDecorator decorator, Control? child)
        where TDecorator : Decorator
    {
        decorator.Child = child;
        return decorator;
    }
}
