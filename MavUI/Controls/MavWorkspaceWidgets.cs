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

/// <summary>File attached to a chat or form: name, size and an optional local path.</summary>
public sealed record MavAttachedFile(string Name, long SizeBytes, string? Path = null)
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
public class MavFileDropZone : MavContentView
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<MavFileDropZone, string>(nameof(Title), "Click to browse or drop files here");

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MavFileDropZone, string?>(nameof(Subtitle), "Supports PDF, PNG, JPG, JSON up to 25 MB");

    public static readonly StyledProperty<bool> AllowMultipleProperty =
        AvaloniaProperty.Register<MavFileDropZone, bool>(nameof(AllowMultiple), true);

    public static readonly StyledProperty<IEnumerable?> FilesSourceProperty =
        AvaloniaProperty.Register<MavFileDropZone, IEnumerable?>(nameof(FilesSource));

    public static readonly StyledProperty<ICommand?> FileAddedCommandProperty =
        AvaloniaProperty.Register<MavFileDropZone, ICommand?>(nameof(FileAddedCommand));

    public static readonly StyledProperty<ICommand?> FileRemovedCommandProperty =
        AvaloniaProperty.Register<MavFileDropZone, ICommand?>(nameof(FileRemovedCommand));

    private readonly AvaloniaList<MavAttachedFile> _files = new();
    private readonly MavCollectionObserver _observer;
    private readonly MavInkSurface _dropSurface;
    private readonly MavIcon _uploadIcon;
    private readonly TextBlock _titleLabel;
    private readonly TextBlock _subtitleLabel;
    private readonly StackPanel _fileListStack;
    private bool _dragOver;
    private bool _syncingFiles;

    public event EventHandler<MavAttachedFile>? FileAdded;
    public event EventHandler<MavAttachedFile>? FileRemoved;
    public event EventHandler? FilesChanged;

    static MavFileDropZone()
    {
        TitleProperty.Changed.AddClassHandler<MavFileDropZone>((z, e) => z._titleLabel.Text = e.GetNewValue<string>());
        SubtitleProperty.Changed.AddClassHandler<MavFileDropZone>((z, e) =>
        {
            var sub = e.GetNewValue<string?>();
            z._subtitleLabel.Text = sub ?? string.Empty;
            z._subtitleLabel.IsVisible = !string.IsNullOrEmpty(sub);
        });
        FilesSourceProperty.Changed.AddClassHandler<MavFileDropZone>((z, e) =>
            z.OnFilesSourceChanged(e.GetNewValue<IEnumerable?>()));
    }

    /// <summary>Creates a drop zone that accepts several files.</summary>
    public MavFileDropZone() : this(null, null, true)
    {
    }

    /// <summary>Creates a drop zone with the given title, subtitle and multi-file setting.</summary>
    public MavFileDropZone(string? title = null, string? subtitle = null, bool allowMultiple = true)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => ReloadFromFilesSource(FilesSource));
        _files.CollectionChanged += (_, _) =>
        {
            if (!_syncingFiles)
                RebuildFileList();
        };
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragEnterEvent, OnDragEnter);
        AddHandler(DragDrop.DragLeaveEvent, OnDragLeave);
        AddHandler(DragDrop.DropEvent, OnDrop);

        _uploadIcon = new MavIcon(MavIcons.CloudUpload, 32)
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

        _dropSurface = new MavInkSurface
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

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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
    public AvaloniaList<MavAttachedFile> Files => _files;

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
            if (item is MavAttachedFile af)
                _files.Add(af);
            else if (item is string name)
                _files.Add(new MavAttachedFile(name, 0));
        }
        _syncingFiles = false;
        RebuildFileList();
    }

    /// <summary>Adds a file to the list and returns the drop zone for chaining.</summary>
    public MavFileDropZone AddFile(string name, long sizeBytes, string? path = null)
    {
        var file = new MavAttachedFile(name, Math.Max(0, sizeBytes), path);
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

    /// <summary>Removes a file from the list and returns <c>true</c> if it was present.</summary>
    public bool RemoveFile(MavAttachedFile file)
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

    /// <summary>Removes all files from the list.</summary>
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

    /// <summary>Opens the platform file picker and adds the chosen files.</summary>
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
        ApplyTheme(MavTheme.Current);
    }

    private void OnDragLeave(object? sender, DragEventArgs e)
    {
        _dragOver = false;
        ApplyTheme(MavTheme.Current);
    }

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        // async void: any exception escaping here would crash the process,
        // so the whole drop pipeline is guarded, not only the awaited call.
        try
        {
            await HandleDropAsync(e);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MavFileDrop: drop handling failed: {ex}");
        }
    }

    private async Task HandleDropAsync(DragEventArgs e)
    {
        _dragOver = false;
        ApplyTheme(MavTheme.Current);

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
                // Size is optional metadata; the file is still added without it.
            }
            AddFile(item.Name, size, item.TryGetLocalPath());
        }
    }

    private void RebuildFileList()
    {
        _fileListStack.Children.Clear();
        _fileListStack.IsVisible = _files.Count > 0;
        if (_files.Count == 0) return;

        var t = MavTheme.Current;
        var c = t.Color;

        foreach (var file in _files)
        {
            var captured = file;
            var icon = new MavIcon(MavIcons.Description, 20)
            {
                UseThemeDefaultColor = false,
                Color = c.Primary,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var nameLbl = new TextBlock
            {
                Text = file.Name,
                Foreground = MavTheme.BrushOf(c.OnSurface),
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
            };
            nameLbl.Apply(MavTypeRole.BodyMedium, t.Typography);

            var sizeLbl = new TextBlock
            {
                Text = file.FormattedSize,
                Foreground = MavTheme.BrushOf(c.OnSurfaceVariant),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0),
            };
            sizeLbl.Apply(MavTypeRole.LabelSmall, t.Typography);

            var removeBtn = new MavIconButton(MavIcons.Close)
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

            var pill = new MavSurface
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

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _dropSurface.FillColor = _dragOver
            ? c.PrimaryContainer.WithAlpha(0.45f)
            : c.SurfaceContainerLowest;
        _dropSurface.StrokeColor = _dragOver ? c.Primary : c.OutlineVariant;
        _dropSurface.RippleColor = c.Primary;

        _uploadIcon.Color = c.Primary;
        _titleLabel.Apply(MavTypeRole.TitleSmall, t.Typography);
        _titleLabel.Foreground = MavTheme.BrushOf(c.OnSurface);
        _subtitleLabel.Apply(MavTypeRole.BodySmall, t.Typography);
        _subtitleLabel.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        RebuildFileList();
    }
}

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
