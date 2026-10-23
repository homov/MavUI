using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Styling;

namespace AMarkup;

/// <summary>
/// A typed Avalonia style that targets <typeparamref name="TControl"/> by default.
/// </summary>
/// <typeparam name="TControl">The styled element type.</typeparam>
public class Style<TControl> : Style
    where TControl : StyledElement
{
    /// <summary>
    /// Creates a style with an <c>OfType&lt;TControl&gt;()</c> selector.
    /// </summary>
    public Style()
    {
        Selector? baseSelector = null;
        Selector = baseSelector!.OfType<TControl>();
    }

    /// <summary>
    /// Creates a typed style. A selector that starts with a concrete type or
    /// contains traversal syntax is used as-is; a simple class or pseudoclass
    /// selector is appended to the implicit <c>OfType&lt;TControl&gt;()</c>
    /// selector.
    /// </summary>
    /// <param name="selector">Selector constraints, for example a class or pseudoclass.</param>
    /// <param name="expression">Captured source expression, used to detect traversal helpers.</param>
    public Style(
        Func<Selector, Selector> selector,
        [CallerArgumentExpression(nameof(selector))] string? expression = null)
    {
        ArgumentNullException.ThrowIfNull(selector);
        Selector = CreateSelector(selector, expression);
    }

    /// <summary>
    /// Adds a setter for the specified Avalonia property.
    /// </summary>
    public Style<TControl> Setter(AvaloniaProperty property, object? value)
    {
        ArgumentNullException.ThrowIfNull(property);
        Setters.Add(new Setter(property, value));
        return this;
    }

    /// <summary>
    /// Replaces the selector with one based on <typeparamref name="TControl"/>.
    /// </summary>
    public Style<TControl> WithSelector(
        Func<Selector, Selector> selector,
        [CallerArgumentExpression(nameof(selector))] string? expression = null)
    {
        ArgumentNullException.ThrowIfNull(selector);
        Selector = CreateSelector(selector, expression);
        return this;
    }

    private static Selector CreateSelector(Func<Selector, Selector> selector, string? expression)
    {
        Selector? baseSelector = null;
        string selectorText = selector(baseSelector!)?.ToString() ?? string.Empty;
        bool hasExplicitRoot = selectorText.Length > 0 && char.IsLetter(selectorText[0]);
        bool isTraversal = selectorText.Contains(' ')
            || selectorText.Contains('>')
            || selectorText.Contains('/')
            || (expression?.Contains(".Descendant(", StringComparison.Ordinal) ?? false)
            || (expression?.Contains(".Child(", StringComparison.Ordinal) ?? false)
            || (expression?.Contains(".Template(", StringComparison.Ordinal) ?? false);

        return hasExplicitRoot || isTraversal
            ? selector(baseSelector!)
            : selector(baseSelector!.OfType<TControl>());
    }
}
