using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Platform;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Column definition for a <see cref="MavDataTable"/>: header, value selector, width and alignment.</summary>
public sealed class MavDataColumn : StyledElement
{
    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<MavDataColumn, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string?> PropertyNameProperty =
        AvaloniaProperty.Register<MavDataColumn, string?>(nameof(PropertyName));

    public static readonly StyledProperty<bool> IsNumericProperty =
        AvaloniaProperty.Register<MavDataColumn, bool>(nameof(IsNumeric));

    public static readonly StyledProperty<bool> IsSortableProperty =
        AvaloniaProperty.Register<MavDataColumn, bool>(nameof(IsSortable), true);

    public static readonly StyledProperty<IDataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<MavDataColumn, IDataTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<GridLength> WidthProperty =
        AvaloniaProperty.Register<MavDataColumn, GridLength>(nameof(Width), new GridLength(1, GridUnitType.Star));

    private Func<object, object?>? _valueSelector;

    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public string? PropertyName
    {
        get => GetValue(PropertyNameProperty);
        set => SetValue(PropertyNameProperty, value);
    }

    public string? BindingPath
    {
        get => PropertyName;
        set => PropertyName = value;
    }

    public Func<object, object?> ValueSelector
    {
        get => _valueSelector ?? ResolveByPropertyName;
        set => _valueSelector = value;
    }

    public bool IsNumeric
    {
        get => GetValue(IsNumericProperty);
        set => SetValue(IsNumericProperty, value);
    }

    public bool IsSortable
    {
        get => GetValue(IsSortableProperty);
        set => SetValue(IsSortableProperty, value);
    }

    [Content]
    public IDataTemplate? CellTemplate
    {
        get => GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    public Func<object, Control>? CellFactory { get; set; }

    public GridLength Width
    {
        get => GetValue(WidthProperty);
        set => SetValue(WidthProperty, value);
    }

    /// <summary>Creates an empty column.</summary>
    public MavDataColumn()
    {
    }

    /// <summary>Creates a column with a header, a value selector and an optional width.</summary>
    public MavDataColumn(
        string header,
        Func<object, object?> valueSelector,
        bool isNumeric = false,
        bool isSortable = true,
        Func<object, Control>? cellTemplate = null,
        GridLength? width = null)
    {
        Header = header;
        _valueSelector = valueSelector;
        IsNumeric = isNumeric;
        IsSortable = isSortable;
        CellFactory = cellTemplate;
        Width = width ?? new GridLength(1, GridUnitType.Star);
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
        "Trimming",
        "IL2072",
        Justification = "PropertyName targets an application model type; trimmed apps must " +
                        "root those properties or use MavDataColumn.ValueSelector.")]
    private object? ResolveByPropertyName(object row)
    {
        if (row is null) return null;
        var propName = !string.IsNullOrEmpty(PropertyName) ? PropertyName : Header;
        if (string.IsNullOrEmpty(propName)) return row;
        var prop = MavPropertyCache.Get(row.GetType(), propName);
        return prop?.GetValue(row);
    }
}
