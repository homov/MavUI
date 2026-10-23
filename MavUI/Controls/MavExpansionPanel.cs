using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 collapsible accordion panel with animated chevron and body container.
/// </summary>
public class MavExpansionPanel : MavContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavExpansionPanel, string>(nameof(Title), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavExpansionPanel, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MavExpansionPanel, string?>(nameof(LeadingIcon));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MavExpansionPanel, bool>(nameof(IsExpanded));

    public static new readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<MavExpansionPanel, object?>(nameof(Content));

    private readonly MavSurface _card;
    private readonly MavInkSurface _header;
    private readonly MavIcon _leadIcon;
    private readonly TextBlock _titleLabel;
    private readonly TextBlock _subtitleLabel;
    private readonly MavIcon _chevron;
    private readonly MavDivider _divider;
    private readonly Border _bodyHost;
    private readonly ContentPresenter _bodyPresenter;

    public event EventHandler<bool>? ExpandedChanged;

    static MavExpansionPanel()
    {
        TitleProperty.Changed.AddClassHandler<MavExpansionPanel>((p, _) => p.Sync());
        SubtitleProperty.Changed.AddClassHandler<MavExpansionPanel>((p, _) => p.Sync());
        LeadingIconProperty.Changed.AddClassHandler<MavExpansionPanel>((p, _) => p.Sync());
        IsExpandedProperty.Changed.AddClassHandler<MavExpansionPanel>((p, e) => p.OnExpandedChanged(e.GetNewValue<bool>()));
        ContentProperty.Changed.AddClassHandler<MavExpansionPanel>((p, e) => p.SetContent(e.GetNewValue<object?>()));
    }

    /// <summary>Creates an empty expansion panel.</summary>
    public MavExpansionPanel() : this(string.Empty)
    {
    }

    /// <summary>Creates an expansion panel with a header, icon and content.</summary>
    public MavExpansionPanel(
        string title = "",
        string? subtitle = null,
        object? content = null,
        bool isExpanded = false,
        string? leadingIcon = null)
    {
        _leadIcon = new MavIcon { Size = 22, IsVisible = false, UseThemeDefaultColor = false, Margin = new Thickness(0, 0, 12, 0) };
        _titleLabel = new TextBlock { TextWrapping = TextWrapping.NoWrap };
        _subtitleLabel = new TextBlock { IsVisible = false, TextWrapping = TextWrapping.NoWrap };

        var titles = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        titles.Children.Add(_titleLabel);
        titles.Children.Add(_subtitleLabel);

        _chevron = new MavIcon(MavIcons.ExpandMore, 24)
        {
            UseThemeDefaultColor = false,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
        };
        headerRow.Add(_leadIcon, 0, 0);
        headerRow.Add(titles, 1, 0);
        headerRow.Add(_chevron, 2, 0);

        _header = new MavInkSurface
        {
            MinHeight = 56,
            CornerRadius = 12,
            Padding = new Thickness(16, 12),
            Content = headerRow,
        };
        _header.Clicked += (_, _) => IsExpanded = !IsExpanded;

        _divider = new MavDivider { IsVisible = isExpanded };
        _bodyPresenter = new ContentPresenter();
        _bodyHost = new Border
        {
            Padding = new Thickness(16, 12, 16, 16),
            IsVisible = isExpanded,
            Child = _bodyPresenter,
        };

        var stack = new StackPanel { Orientation = Orientation.Vertical };
        stack.Children.Add(_header);
        stack.Children.Add(_divider);
        stack.Children.Add(_bodyHost);

        _card = new MavSurface
        {
            Radius = 12,
            BorderStrokeThickness = 1,
            ClipToBounds = true,
            Content = stack,
        };

        base.Content = _card;

        Title = title;
        Subtitle = subtitle;
        LeadingIcon = leadingIcon;
        if (content is not null) Content = content;
        if (isExpanded)
        {
            _chevron.SetRotation(180);
            SetValue(IsExpandedProperty, true);
        }

        Sync();
        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Header
    {
        get => Title;
        set => Title = value;
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>The panel body, shown when expanded. Any object is accepted, as in Avalonia's ContentControl.</summary>
    [Content]
    public new object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }

    private void SetContent(object? content)
    {
        if (content is Control control)
            MavVisualTreeHelpers.DetachFromParent(control);
        _bodyPresenter.Content = content;
    }

    private void Sync()
    {
        _titleLabel.Text = Title;
        _subtitleLabel.Text = Subtitle ?? string.Empty;
        _subtitleLabel.IsVisible = !string.IsNullOrEmpty(Subtitle);
        _leadIcon.Glyph = LeadingIcon;
        _leadIcon.IsVisible = !string.IsNullOrEmpty(LeadingIcon);
    }

    private void OnExpandedChanged(bool expanded)
    {
        MavMotion.Animate(this, "exp_chev", _chevron.GetRotation(), expanded ? 180 : 0, MavMotion.Short4, MavMotion.CubicOut, v =>
        {
            _chevron.SetRotation(v);
        });

        _divider.IsVisible = expanded;
        if (expanded)
        {
            _bodyHost.IsVisible = true;
            _bodyHost.Opacity = 0;
            MavMotion.Animate(this, "exp_body", 0, 1, MavMotion.Short4, MavMotion.CubicOut, v => _bodyHost.Opacity = v);
        }
        else
        {
            MavMotion.Animate(this, "exp_body", _bodyHost.Opacity, 0, MavMotion.Short3, MavMotion.CubicIn, v => _bodyHost.Opacity = v, () =>
            {
                if (!IsExpanded) _bodyHost.IsVisible = false;
            });
        }

        ApplyTheme(MavTheme.Current);
        ExpandedChanged?.Invoke(this, expanded);
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _titleLabel.Apply(MavTypeRole.TitleSmall, t.Typography);
        _subtitleLabel.Apply(MavTypeRole.BodySmall, t.Typography);

        _card.FillColor = IsExpanded ? c.SurfaceContainerLow : c.Surface;
        _card.BorderStrokeColor = c.OutlineVariant;
        var innerR = Math.Max(0, _card.Radius - _card.BorderStrokeThickness);
        _header.CornerRadii = IsExpanded ? new CornerRadius(innerR, innerR, 0, 0) : new CornerRadius(innerR);
        _header.RippleColor = c.OnSurface;
        _titleLabel.Foreground = MavTheme.BrushOf(c.OnSurface);
        _subtitleLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        _leadIcon.Color = IsExpanded ? c.Primary : c.OnSurfaceVariant;
        _chevron.Color = c.OnSurfaceVariant;
    }
}
