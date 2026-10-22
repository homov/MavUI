using System.Collections;
using System.Diagnostics;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Chat / Comment Message Bubble with asymmetric Expressive corner radii,
/// sender avatar, timestamp, and read receipt indicator.
/// </summary>
public class MavChatBubble : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavChatBubble, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> SenderNameProperty =
        AvaloniaProperty.Register<MavChatBubble, string?>(nameof(SenderName));

    public static readonly StyledProperty<string?> TimestampProperty =
        AvaloniaProperty.Register<MavChatBubble, string?>(nameof(Timestamp));

    public static readonly StyledProperty<string?> AvatarInitialsProperty =
        AvaloniaProperty.Register<MavChatBubble, string?>(nameof(AvatarInitials));

    public static readonly StyledProperty<bool> IsOutgoingProperty =
        AvaloniaProperty.Register<MavChatBubble, bool>(nameof(IsOutgoing));

    public static readonly StyledProperty<bool> IsReadProperty =
        AvaloniaProperty.Register<MavChatBubble, bool>(nameof(IsRead), true);

    private readonly Grid _outerRow;
    private readonly MavAvatar _avatar;
    private readonly Border _bubbleBorder;
    private readonly TextBlock _senderLabel;
    private readonly TextBlock _textLabel;
    private readonly TextBlock _timeLabel;
    private readonly MavIcon _readIcon;

    static MavChatBubble()
    {
        TextProperty.Changed.AddClassHandler<MavChatBubble>((b, _) => b.Sync());
        SenderNameProperty.Changed.AddClassHandler<MavChatBubble>((b, _) => b.Sync());
        TimestampProperty.Changed.AddClassHandler<MavChatBubble>((b, _) => b.Sync());
        AvatarInitialsProperty.Changed.AddClassHandler<MavChatBubble>((b, _) => b.Sync());
        IsOutgoingProperty.Changed.AddClassHandler<MavChatBubble>((b, _) => b.Sync());
        IsReadProperty.Changed.AddClassHandler<MavChatBubble>((b, _) => b.Sync());
    }

    /// <summary>Creates an empty chat bubble.</summary>
    public MavChatBubble() : this(string.Empty)
    {
    }

    /// <summary>Creates a chat bubble with the given text, sender side and optional time.</summary>
    public MavChatBubble(
        string text = "",
        bool isOutgoing = false,
        string? senderName = null,
        string? timestamp = null,
        string? avatarInitials = null,
        bool isRead = true)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _avatar = new MavAvatar(avatarInitials ?? "U", size: 32)
        {
            VerticalAlignment = VerticalAlignment.Bottom,
            Margin = new Thickness(0, 0, 8, 2),
        };

        _senderLabel = new TextBlock
        {
            FontWeight = FontWeight.SemiBold,
            IsVisible = false,
        };

        _textLabel = new TextBlock
        {
            TextWrapping = TextWrapping.Wrap,
        };

        _timeLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
        };

        _readIcon = new MavIcon(MavIcons.DoneAll, 14)
        {
            UseThemeDefaultColor = false,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 0, 0, 0),
        };

        var metaRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 2, 0, 0),
            Children = { _timeLabel, _readIcon },
        };

        var bubbleStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            Children = { _senderLabel, _textLabel, metaRow },
        };

        _bubbleBorder = new Border
        {
            MaxWidth = 420,
            Padding = new Thickness(14, 10),
            Child = bubbleStack,
        };

        _outerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
        };
        _outerRow.Add(_avatar, 0, 0);
        _outerRow.Add(_bubbleBorder, 1, 0);

        Content = _outerRow;

        Text = text;
        IsOutgoing = isOutgoing;
        SenderName = senderName;
        Timestamp = timestamp;
        AvatarInitials = avatarInitials;
        IsRead = isRead;

        Sync();
        MavTheme.Observe(this, (self, _) => self.Sync());
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? SenderName
    {
        get => GetValue(SenderNameProperty);
        set => SetValue(SenderNameProperty, value);
    }

    public string? Sender
    {
        get => SenderName;
        set => SenderName = value;
    }

    public string? Timestamp
    {
        get => GetValue(TimestampProperty);
        set => SetValue(TimestampProperty, value);
    }

    public string? AvatarInitials
    {
        get => GetValue(AvatarInitialsProperty);
        set => SetValue(AvatarInitialsProperty, value);
    }

    public bool IsOutgoing
    {
        get => GetValue(IsOutgoingProperty);
        set => SetValue(IsOutgoingProperty, value);
    }

    public bool IsRead
    {
        get => GetValue(IsReadProperty);
        set => SetValue(IsReadProperty, value);
    }

    private void Sync()
    {
        var t = MavTheme.Current;
        var c = t.Color;
        var outgoing = IsOutgoing;

        _avatar.IsVisible = !outgoing && !string.IsNullOrEmpty(AvatarInitials);
        if (!string.IsNullOrEmpty(AvatarInitials))
            _avatar.Initials = AvatarInitials;

        _senderLabel.Text = SenderName ?? string.Empty;
        _senderLabel.IsVisible = !outgoing && !string.IsNullOrEmpty(SenderName);
        _senderLabel.Apply(MavTypeRole.LabelMedium, t.Typography);
        _senderLabel.Foreground = MavTheme.BrushOf(c.Primary);

        _textLabel.Text = Text;
        _textLabel.Apply(MavTypeRole.BodyMedium, t.Typography);

        _timeLabel.Text = Timestamp ?? string.Empty;
        _timeLabel.IsVisible = !string.IsNullOrEmpty(Timestamp);
        _timeLabel.Apply(MavTypeRole.LabelSmall, t.Typography);

        _readIcon.IsVisible = outgoing;
        _readIcon.Glyph = IsRead ? MavIcons.DoneAll : MavIcons.Done;

        var bg = outgoing ? c.PrimaryContainer : c.SurfaceContainerHigh;
        var fg = outgoing ? c.OnPrimaryContainer : c.OnSurface;
        var subFg = outgoing ? c.OnPrimaryContainer.WithAlpha(0.72f) : c.OnSurfaceVariant;

        _bubbleBorder.Background = MavTheme.BrushOf(bg);
        _bubbleBorder.CornerRadius = outgoing
            ? new CornerRadius(20, 20, 4, 20)
            : new CornerRadius(20, 20, 20, 4);
        _bubbleBorder.HorizontalAlignment = outgoing
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Left;

        _textLabel.Foreground = MavTheme.BrushOf(fg);
        _timeLabel.Foreground = MavTheme.BrushOf(subFg);
        _readIcon.Color = IsRead ? c.Primary : subFg;
    }
}
