using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

public sealed record MdAttachedFile(string Name, long SizeBytes, string? Path = null)
{
    public string FormattedSize =>
        SizeBytes >= 1024 * 1024
            ? $"{SizeBytes / (1024.0 * 1024.0):0.0} MB"
            : (SizeBytes >= 1024 ? $"{SizeBytes / 1024.0:0.0} KB" : $"{SizeBytes} B");
}

/// <summary>
/// Material Design 3 File Drop Zone &amp; Attachment Picker with native <see cref="IStorageProvider"/>
/// integration, drag-over highlight, and removable attached file rows.
/// </summary>
public class MdFileDropZone : MdContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MdFileDropZone, string>(nameof(Title), "Click to browse or drop files here");

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MdFileDropZone, string?>(nameof(Subtitle), "Supports PDF, PNG, JPG, JSON up to 25 MB");

    public static readonly StyledProperty<bool> AllowMultipleProperty =
        AvaloniaProperty.Register<MdFileDropZone, bool>(nameof(AllowMultiple), true);

    public static readonly StyledProperty<IEnumerable?> FilesSourceProperty =
        AvaloniaProperty.Register<MdFileDropZone, IEnumerable?>(nameof(FilesSource));

    public static readonly StyledProperty<ICommand?> FileAddedCommandProperty =
        AvaloniaProperty.Register<MdFileDropZone, ICommand?>(nameof(FileAddedCommand));

    public static readonly StyledProperty<ICommand?> FileRemovedCommandProperty =
        AvaloniaProperty.Register<MdFileDropZone, ICommand?>(nameof(FileRemovedCommand));

    private readonly AvaloniaList<MdAttachedFile> _files = new();
    private readonly MdCollectionObserver _observer;
    private readonly MdInkSurface _dropSurface;
    private readonly MdIcon _uploadIcon;
    private readonly TextBlock _titleLabel;
    private readonly TextBlock _subtitleLabel;
    private readonly StackPanel _fileListStack;
    private bool _dragOver;
    private bool _syncingFiles;

    public event EventHandler<MdAttachedFile>? FileAdded;
    public event EventHandler<MdAttachedFile>? FileRemoved;
    public event EventHandler? FilesChanged;

    static MdFileDropZone()
    {
        TitleProperty.Changed.AddClassHandler<MdFileDropZone>((z, e) => z._titleLabel.Text = e.GetNewValue<string>());
        SubtitleProperty.Changed.AddClassHandler<MdFileDropZone>((z, e) =>
        {
            var sub = e.GetNewValue<string?>();
            z._subtitleLabel.Text = sub ?? string.Empty;
            z._subtitleLabel.IsVisible = !string.IsNullOrEmpty(sub);
        });
        FilesSourceProperty.Changed.AddClassHandler<MdFileDropZone>((z, e) =>
            z.OnFilesSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    public MdFileDropZone() : this(null, null, true)
    {
    }

    public MdFileDropZone(string? title = null, string? subtitle = null, bool allowMultiple = true)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MdCollectionObserver(() => ReloadFromFilesSource(FilesSource));
        _files.CollectionChanged += (_, _) =>
        {
            if (!_syncingFiles)
                RebuildFileList();
        };
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);

        _uploadIcon = new MdIcon(MdIcons.CloudUpload, 32)
        {
            UseThemeDefaultColor = false,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        _titleLabel = new TextBlock
        {
            Text = title ?? Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };

        _subtitleLabel = new TextBlock
        {
            Text = subtitle ?? Subtitle ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(subtitle ?? Subtitle),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap,
        };

        var promptStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children = { _uploadIcon, _titleLabel, _subtitleLabel },
        };

        _dropSurface = new MdInkSurface
        {
            CornerRadius = 16,
            StrokeThickness = 1.5,
            Padding = new Thickness(24, 20),
            Content = promptStack,
        };
        _dropSurface.Clicked += async (_, _) => await BrowseFilesAsync();

        _fileListStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 6,
            IsVisible = false,
        };

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 10,
            Children = { _dropSurface, _fileListStack },
        };

        Content = root;
        if (title is not null) Title = title;
        if (subtitle is not null) Subtitle = subtitle;
        AllowMultiple = allowMultiple;

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool AllowMultiple
    {
        get => GetValue(AllowMultipleProperty);
        set => SetValue(AllowMultipleProperty, value);
    }

    public IEnumerable? FilesSource
    {
        get => GetValue(FilesSourceProperty);
        set => SetValue(FilesSourceProperty, value);
    }

    public ICommand? FileAddedCommand
    {
        get => GetValue(FileAddedCommandProperty);
        set => SetValue(FileAddedCommandProperty, value);
    }

    public ICommand? FileRemovedCommand
    {
        get => GetValue(FileRemovedCommandProperty);
        set => SetValue(FileRemovedCommandProperty, value);
    }

    [Content]
    public AvaloniaList<MdAttachedFile> Files => _files;

    private void OnFilesSourceChanged(IEnumerable? src)
    {
        _observer.Attach(src);
        ReloadFromFilesSource(src);
    }

    private void ReloadFromFilesSource(IEnumerable? src)
    {
        if (_syncingFiles || src is null) return;
        _syncingFiles = true;
        _files.Clear();
        foreach (var item in src)
        {
            if (item is MdAttachedFile af)
                _files.Add(af);
            else if (item is string name)
                _files.Add(new MdAttachedFile(name, 0));
        }
        _syncingFiles = false;
        RebuildFileList();
    }

    public MdFileDropZone AddFile(string name, long sizeBytes, string? path = null)
    {
        var file = new MdAttachedFile(name, Math.Max(0, sizeBytes), path);
        _syncingFiles = true;
        if (!AllowMultiple)
        {
            _files.Clear();
            if (FilesSource is IList srcListClear && !srcListClear.IsReadOnly)
                srcListClear.Clear();
        }
        _files.Add(file);
        if (FilesSource is IList srcList && !srcList.IsReadOnly)
            srcList.Add(file);
        _syncingFiles = false;

        RebuildFileList();
        FileAdded?.Invoke(this, file);
        if (FileAddedCommand is { } aCmd && aCmd.CanExecute(file))
            aCmd.Execute(file);
        FilesChanged?.Invoke(this, EventArgs.Empty);
        return this;
    }

    public bool RemoveFile(MdAttachedFile file)
    {
        _syncingFiles = true;
        var removed = _files.Remove(file);
        if (removed && FilesSource is IList srcList && !srcList.IsReadOnly)
            srcList.Remove(file);
        _syncingFiles = false;
        if (!removed) return false;
        RebuildFileList();
        FileRemoved?.Invoke(this, file);
        if (FileRemovedCommand is { } rCmd && rCmd.CanExecute(file))
            rCmd.Execute(file);
        FilesChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public void ClearFiles()
    {
        if (_files.Count == 0) return;
        _syncingFiles = true;
        _files.Clear();
        if (FilesSource is IList srcList && !srcList.IsReadOnly)
            srcList.Clear();
        _syncingFiles = false;
        RebuildFileList();
        FilesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task BrowseFilesAsync()
    {
        var top = TopLevel.GetTopLevel(this);
        if (top?.StorageProvider is not { CanOpen: true } sp) return;

        var picked = await sp.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = Title,
            AllowMultiple = AllowMultiple,
        });

        foreach (var item in picked)
        {
            long size = 0;
            try
            {
                var props = await item.GetBasicPropertiesAsync();
                size = (long)(props.Size ?? 0);
            }
            catch
            {
                // Ignore if platform storage provider does not expose file size
            }
            AddFile(item.Name, size, item.TryGetLocalPath());
        }
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        _dragOver = true;
        ApplyTheme(MdTheme.Current);
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        _dragOver = false;
        ApplyTheme(MdTheme.Current);
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        _dragOver = false;
        ApplyTheme(MdTheme.Current);

        var items = e.DataTransfer.TryGetFiles();
        if (items is null) return;
        foreach (var item in items)
        {
            long size = 0;
            try
            {
                var props = await item.GetBasicPropertiesAsync();
                size = (long)(props.Size ?? 0);
            }
            catch
            {
            }
            AddFile(item.Name, size, item.TryGetLocalPath());
        }
    }

    private void RebuildFileList()
    {
        _fileListStack.Children.Clear();
        _fileListStack.IsVisible = _files.Count > 0;
        if (_files.Count == 0) return;

        var t = MdTheme.Current;
        var c = t.Color;

        foreach (var file in _files)
        {
            var captured = file;
            var icon = new MdIcon(MdIcons.Description, 20)
            {
                UseThemeDefaultColor = false,
                Color = c.Primary,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var nameLbl = new TextBlock
            {
                Text = file.Name,
                Foreground = MdTheme.BrushOf(c.OnSurface),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            nameLbl.Apply(MdTypeRole.BodyMedium, t.Typography);

            var sizeLbl = new TextBlock
            {
                Text = file.FormattedSize,
                Foreground = MdTheme.BrushOf(c.OnSurfaceVariant),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
            };
            sizeLbl.Apply(MdTypeRole.LabelSmall, t.Typography);

            var removeBtn = new MdIconButton(MdIcons.Close)
            {
                ButtonSize = 32,
            };
            removeBtn.Clicked += (_, _) => RemoveFile(captured);

            var row = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
                VerticalAlignment = VerticalAlignment.Center,
            };
            row.Add(icon, 0, 0);
            nameLbl.Margin = new Thickness(10, 0, 0, 0);
            row.Add(nameLbl, 1, 0);
            row.Add(sizeLbl, 2, 0);
            row.Add(removeBtn, 3, 0);

            var pill = new MdSurface
            {
                Radius = 12,
                Padding = new Thickness(12, 6, 6, 6),
                FillColor = c.SurfaceContainerLow,
                BorderStrokeColor = c.OutlineVariant,
                BorderStrokeThickness = 1,
                Content = row,
            };
            _fileListStack.Children.Add(pill);
        }
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _dropSurface.FillColor = _dragOver
            ? c.PrimaryContainer.WithAlpha(0.45f)
            : c.SurfaceContainerLowest;
        _dropSurface.StrokeColor = _dragOver ? c.Primary : c.OutlineVariant;
        _dropSurface.RippleColor = c.Primary;

        _uploadIcon.Color = c.Primary;
        _titleLabel.Apply(MdTypeRole.TitleSmall, t.Typography);
        _titleLabel.Foreground = MdTheme.BrushOf(c.OnSurface);
        _subtitleLabel.Apply(MdTypeRole.BodySmall, t.Typography);
        _subtitleLabel.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);

        RebuildFileList();
    }
}

/// <summary>
/// Material Design 3 Chat / Comment Message Bubble with asymmetric Expressive corner radii,
/// sender avatar, timestamp, and read receipt indicator.
/// </summary>
public class MdChatBubble : MdContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdChatBubble, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> SenderNameProperty =
        AvaloniaProperty.Register<MdChatBubble, string?>(nameof(SenderName));

    public static readonly StyledProperty<string?> TimestampProperty =
        AvaloniaProperty.Register<MdChatBubble, string?>(nameof(Timestamp));

    public static readonly StyledProperty<string?> AvatarInitialsProperty =
        AvaloniaProperty.Register<MdChatBubble, string?>(nameof(AvatarInitials));

    public static readonly StyledProperty<bool> IsOutgoingProperty =
        AvaloniaProperty.Register<MdChatBubble, bool>(nameof(IsOutgoing));

    public static readonly StyledProperty<bool> IsReadProperty =
        AvaloniaProperty.Register<MdChatBubble, bool>(nameof(IsRead), true);

    private readonly Grid _outerRow;
    private readonly MdAvatar _avatar;
    private readonly Border _bubbleBorder;
    private readonly TextBlock _senderLabel;
    private readonly TextBlock _textLabel;
    private readonly TextBlock _timeLabel;
    private readonly MdIcon _readIcon;

    static MdChatBubble()
    {
        TextProperty.Changed.AddClassHandler<MdChatBubble>((b, _) => b.Sync());
        SenderNameProperty.Changed.AddClassHandler<MdChatBubble>((b, _) => b.Sync());
        TimestampProperty.Changed.AddClassHandler<MdChatBubble>((b, _) => b.Sync());
        AvatarInitialsProperty.Changed.AddClassHandler<MdChatBubble>((b, _) => b.Sync());
        IsOutgoingProperty.Changed.AddClassHandler<MdChatBubble>((b, _) => b.Sync());
        IsReadProperty.Changed.AddClassHandler<MdChatBubble>((b, _) => b.Sync());
    }

    public MdChatBubble() : this(string.Empty)
    {
    }

    public MdChatBubble(
        string text = "",
        bool isOutgoing = false,
        string? senderName = null,
        string? timestamp = null,
        string? avatarInitials = null,
        bool isRead = true)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _avatar = new MdAvatar(avatarInitials ?? "U", size: 32)
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

        _readIcon = new MdIcon(MdIcons.DoneAll, 14)
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
        MdTheme.Observe(this, (self, _) => self.Sync());
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
        var t = MdTheme.Current;
        var c = t.Color;
        var outgoing = IsOutgoing;

        _avatar.IsVisible = !outgoing && !string.IsNullOrEmpty(AvatarInitials);
        if (!string.IsNullOrEmpty(AvatarInitials))
            _avatar.Initials = AvatarInitials;

        _senderLabel.Text = SenderName ?? string.Empty;
        _senderLabel.IsVisible = !outgoing && !string.IsNullOrEmpty(SenderName);
        _senderLabel.Apply(MdTypeRole.LabelMedium, t.Typography);
        _senderLabel.Foreground = MdTheme.BrushOf(c.Primary);

        _textLabel.Text = Text;
        _textLabel.Apply(MdTypeRole.BodyMedium, t.Typography);

        _timeLabel.Text = Timestamp ?? string.Empty;
        _timeLabel.IsVisible = !string.IsNullOrEmpty(Timestamp);
        _timeLabel.Apply(MdTypeRole.LabelSmall, t.Typography);

        _readIcon.IsVisible = outgoing;
        _readIcon.Glyph = IsRead ? MdIcons.DoneAll : MdIcons.Done;

        var bg = outgoing ? c.PrimaryContainer : c.SurfaceContainerHigh;
        var fg = outgoing ? c.OnPrimaryContainer : c.OnSurface;
        var subFg = outgoing ? c.OnPrimaryContainer.WithAlpha(0.72f) : c.OnSurfaceVariant;

        _bubbleBorder.Background = MdTheme.BrushOf(bg);
        _bubbleBorder.CornerRadius = outgoing
            ? new CornerRadius(20, 20, 4, 20)
            : new CornerRadius(20, 20, 20, 4);
        _bubbleBorder.HorizontalAlignment = outgoing
            ? HorizontalAlignment.Right
            : HorizontalAlignment.Left;

        _textLabel.Foreground = MdTheme.BrushOf(fg);
        _timeLabel.Foreground = MdTheme.BrushOf(subFg);
        _readIcon.Color = IsRead ? c.Primary : subFg;
    }
}
