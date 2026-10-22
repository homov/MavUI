using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Declarative suggestion or recent-query item for <see cref="MavSearchView"/>.
/// </summary>
public sealed class MavSearchSuggestion : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavSearchSuggestion, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavSearchSuggestion, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MavSearchSuggestion, string>(nameof(Icon), MavIcons.Search);

    public static readonly StyledProperty<bool> IsRecentProperty =
        AvaloniaProperty.Register<MavSearchSuggestion, bool>(nameof(IsRecent));

    /// <summary>Creates an empty search suggestion.</summary>
    public MavSearchSuggestion()
    {
    }

    /// <summary>Creates a suggestion with the given text, optional subtitle and icon.</summary>
    public MavSearchSuggestion(string text, string? subtitle = null, string icon = MavIcons.Search, bool isRecent = false)
    {
        Text = text;
        Subtitle = subtitle;
        Icon = icon;
        IsRecent = isRecent;
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsRecent
    {
        get => GetValue(IsRecentProperty);
        set => SetValue(IsRecentProperty, value);
    }

    /// <summary>
    /// Measures the suggestion during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;
}
