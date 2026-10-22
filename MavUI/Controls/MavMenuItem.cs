using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>Entry of a <see cref="MavMenu"/>: an action, a divider or a submenu.</summary>
public sealed class MavMenuItem : Control, IEnumerable<MavMenuItem>
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavMenuItem, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MavMenuItem, string?>(nameof(LeadingIcon));

    public static readonly StyledProperty<string?> TrailingTextProperty =
        AvaloniaProperty.Register<MavMenuItem, string?>(nameof(TrailingText));

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavMenuItem, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<MavMenuItem, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MavMenuItem, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsDividerProperty =
        AvaloniaProperty.Register<MavMenuItem, bool>(nameof(IsDivider));

    private AvaloniaList<MavMenuItem>? _subItems;

    static MavMenuItem()
    {
        TextProperty.Changed.AddClassHandler<MavMenuItem>((i, e) =>
            MavAutomation.SetName(i, e.GetNewValue<string>()));
    }

    /// <summary>Creates an empty menu entry.</summary>
    public MavMenuItem()
    {
        MavAutomation.SetControlType(this, AutomationControlType.MenuItem);
    }

    /// <summary>Creates a menu entry with text, icon and click handler.</summary>
    public MavMenuItem(
        string text,
        string? leadingIcon = null,
        string? trailingText = null,
        Action? onSelected = null,
        bool isEnabled = true,
        bool isSelected = false)
    {
        MavAutomation.SetControlType(this, AutomationControlType.MenuItem);
        Text = text;
        LeadingIcon = leadingIcon;
        TrailingText = trailingText;
        OnSelected = onSelected;
        IsEnabled = isEnabled;
        IsSelected = isSelected;
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public string? TrailingText
    {
        get => GetValue(TrailingTextProperty);
        set => SetValue(TrailingTextProperty, value);
    }

    public Action? OnSelected { get; set; }
    public event EventHandler? Clicked;

    /// <summary>
    /// Raises the Clicked event for this item.
    /// </summary>
    internal void RaiseClicked() => Clicked?.Invoke(this, EventArgs.Empty);

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool IsDivider
    {
        get => GetValue(IsDividerProperty);
        set => SetValue(IsDividerProperty, value);
    }

    [Content]
    public IList<MavMenuItem>? SubItems
    {
        get
        {
            if (_subItems is null)
            {
                _subItems = new AvaloniaList<MavMenuItem>();
                _subItems.CollectionChanged += (_, _) =>
                {
                    foreach (var s in _subItems)
                        MavVisualTreeHelpers.AttachLogicalChild(this, s);
                };
            }
            return _subItems;
        }
        set
        {
            _subItems = value is null ? null : new AvaloniaList<MavMenuItem>(value);
            if (_subItems is not null)
            {
                foreach (var s in _subItems)
                    MavVisualTreeHelpers.AttachLogicalChild(this, s);
            }
        }
    }

    internal bool HasSubItems => _subItems is { Count: > 0 };

    /// <summary>
    /// Adds a sub-item to this item.
    /// </summary>
    public void Add(MavMenuItem subItem) => SubItems!.Add(subItem);

    /// <summary>
    /// Returns an enumerator over the sub-items.
    /// </summary>
    public IEnumerator<MavMenuItem> GetEnumerator() => (_subItems ?? Enumerable.Empty<MavMenuItem>()).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
