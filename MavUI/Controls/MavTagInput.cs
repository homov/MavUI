using System.Collections;
using System.Globalization;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Tag / Chip Input control that converts typed entries into removable
/// <see cref="MavChip"/> items when pressing Enter or comma.
/// </summary>
public class MavTagInput : MavContentView, IEnumerable<string>
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MavTagInput, string?>(nameof(Label));

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<MavTagInput, string>(nameof(Placeholder), "Add tag…");

    public static readonly StyledProperty<int> MaxTagsProperty =
        AvaloniaProperty.Register<MavTagInput, int>(nameof(MaxTags), 0);

    public static readonly StyledProperty<bool> AllowDuplicatesProperty =
        AvaloniaProperty.Register<MavTagInput, bool>(nameof(AllowDuplicates));

    public static readonly StyledProperty<System.Collections.IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MavTagInput, System.Collections.IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<ICommand?> TagAddedCommandProperty =
        AvaloniaProperty.Register<MavTagInput, ICommand?>(nameof(TagAddedCommand));

    public static readonly StyledProperty<ICommand?> TagRemovedCommandProperty =
        AvaloniaProperty.Register<MavTagInput, ICommand?>(nameof(TagRemovedCommand));

    private readonly AvaloniaList<string> _tags = new();
    private readonly MavCollectionObserver _observer;
    private readonly MavSurface _container;
    private readonly TextBlock _label;
    private readonly WrapPanel _wrap;
    private readonly MavNativeEntry _entry;
    private bool _syncingTags;

    public event EventHandler<string>? TagAdded;
    public event EventHandler<string>? TagRemoved;
    public event EventHandler? TagsChanged;

    static MavTagInput()
    {
        LabelProperty.Changed.AddClassHandler<MavTagInput>((t, e) =>
        {
            var txt = e.GetNewValue<string?>();
            t._label.Text = txt ?? string.Empty;
            t._label.IsVisible = !string.IsNullOrEmpty(txt);
        });
        PlaceholderProperty.Changed.AddClassHandler<MavTagInput>((t, e) =>
            t._entry.Placeholder = e.GetNewValue<string>());
        ItemsSourceProperty.Changed.AddClassHandler<MavTagInput>((t, e) =>
            t.OnItemsSourceChanged(e.GetNewValue<System.Collections.IEnumerable?>()));
    }

    /// <summary>Creates an empty tag input with the default placeholder.</summary>
    public MavTagInput() : this(null, "Add tag…", null)
    {
    }

    /// <summary>Creates a tag input with an optional placeholder and initial tags.</summary>
    public MavTagInput(
        string? label = null,
        string placeholder = "Add tag…",
        IEnumerable<string>? initialTags = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MavCollectionObserver(this, () => ReloadFromItemsSource(ItemsSource));
        _tags.CollectionChanged += (_, _) =>
        {
            if (!_syncingTags)
                RebuildChips();
        };

        _label = new TextBlock
        {
            Text = label ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(label),
            Margin = new Thickness(4, 0, 4, 4),
        };

        _entry = new MavNativeEntry
        {
            Placeholder = placeholder,
            MinWidth = 110,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(4, 2),
        };
        TextInputOptions.SetContentType(_entry, TextInputContentType.Normal);
        TextInputOptions.SetReturnKeyType(_entry, TextInputReturnKeyType.Done);
        _entry.Completed += (_, _) => CommitCurrentInput();
        _entry.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Back && string.IsNullOrEmpty(_entry.Text) && _tags.Count > 0)
            {
                RemoveTagAt(_tags.Count - 1);
                e.Handled = true;
            }
        };
        _entry.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty && _entry.Text is { } txt && txt.Contains(','))
            {
                var parts = txt.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                _entry.Text = string.Empty;
                foreach (var p in parts)
                    AddTag(p);
            }
            else if (e.Property == IsFocusedProperty)
            {
                ApplyTheme(MavTheme.Current);
            }
        };

        _wrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _wrap.Children.Add(_entry);

        _container = new MavSurface
        {
            MinHeight = 52,
            Radius = 12,
            Padding = new Thickness(10, 8),
            BorderStrokeThickness = 1,
            Content = _wrap,
        };
        _container.PointerPressed += (_, _) =>
        {
            if (IsEnabled && !_entry.IsFocused)
                _entry.Focus();
        };

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        root.Children.Add(_label);
        root.Children.Add(_container);

        Content = root;

        if (label is not null) Label = label;
        Placeholder = placeholder;
        if (initialTags is not null)
        {
            foreach (var tag in initialTags)
                AddTag(tag);
        }

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public int MaxTags
    {
        get => GetValue(MaxTagsProperty);
        set => SetValue(MaxTagsProperty, Math.Max(0, value));
    }

    public bool AllowDuplicates
    {
        get => GetValue(AllowDuplicatesProperty);
        set => SetValue(AllowDuplicatesProperty, value);
    }

    public System.Collections.IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? TagAddedCommand
    {
        get => GetValue(TagAddedCommandProperty);
        set => SetValue(TagAddedCommandProperty, value);
    }

    public ICommand? TagRemovedCommand
    {
        get => GetValue(TagRemovedCommandProperty);
        set => SetValue(TagRemovedCommandProperty, value);
    }

    [Content]
    public AvaloniaList<string> Tags => _tags;

    private void OnItemsSourceChanged(System.Collections.IEnumerable? src)
    {
        _observer.Attach(src);
        ReloadFromItemsSource(src);
    }

    private void ReloadFromItemsSource(System.Collections.IEnumerable? src)
    {
        if (_syncingTags || src is null) return;
        _syncingTags = true;
        _tags.Clear();
        foreach (var item in src)
        {
            if (item is not null)
                _tags.Add(item.ToString() ?? string.Empty);
        }
        _syncingTags = false;
        RebuildChips();
    }

    /// <summary>Adds a tag if it is not already present and returns the input for chaining.</summary>
    public MavTagInput AddTag(string tag)
    {
        var clean = tag.Trim();
        if (string.IsNullOrEmpty(clean))
            return this;
        if (MaxTags > 0 && _tags.Count >= MaxTags)
            return this;
        if (!AllowDuplicates && _tags.Contains(clean, StringComparer.OrdinalIgnoreCase))
            return this;

        _syncingTags = true;
        _tags.Add(clean);
        if (ItemsSource is System.Collections.IList srcList && !srcList.IsReadOnly)
            srcList.Add(clean);
        _syncingTags = false;

        RebuildChips();
        TagAdded?.Invoke(this, clean);
        if (TagAddedCommand is { } aCmd && aCmd.CanExecute(clean))
            aCmd.Execute(clean);
        TagsChanged?.Invoke(this, EventArgs.Empty);
        return this;
    }

    /// <summary>
    /// Adds a tag.
    /// </summary>
    public void Add(string tag) => AddTag(tag);

    /// <summary>
    /// Returns an enumerator over the tags.
    /// </summary>
    public IEnumerator<string> GetEnumerator() => _tags.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Removes a tag and returns <c>true</c> if it was present.</summary>
    public bool RemoveTag(string tag)
    {
        int idx = -1;
        for (int i = 0; i < _tags.Count; i++)
        {
            if (string.Equals(_tags[i], tag, StringComparison.OrdinalIgnoreCase))
            {
                idx = i;
                break;
            }
        }
        if (idx < 0) return false;
        RemoveTagAt(idx);
        return true;
    }

    /// <summary>Removes all tags.</summary>
    public void ClearTags()
    {
        if (_tags.Count == 0) return;
        _syncingTags = true;
        _tags.Clear();
        if (ItemsSource is System.Collections.IList srcList && !srcList.IsReadOnly)
            srcList.Clear();
        _syncingTags = false;
        RebuildChips();
        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void CommitCurrentInput()
    {
        var raw = _entry.Text?.Trim();
        if (!string.IsNullOrEmpty(raw))
        {
            _entry.Text = string.Empty;
            AddTag(raw);
        }
    }

    private void RemoveTagAt(int index)
    {
        if (index < 0 || index >= _tags.Count) return;
        var removed = _tags[index];
        _syncingTags = true;
        _tags.RemoveAt(index);
        if (ItemsSource is System.Collections.IList srcList && !srcList.IsReadOnly)
            srcList.Remove(removed);
        _syncingTags = false;
        RebuildChips();
        TagRemoved?.Invoke(this, removed);
        if (TagRemovedCommand is { } rCmd && rCmd.CanExecute(removed))
            rCmd.Execute(removed);
        TagsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RebuildChips()
    {
        _wrap.Children.Clear();
        for (int i = 0; i < _tags.Count; i++)
        {
            var tagText = _tags[i];
            var chip = new MavChip(tagText, MavChipKind.Input)
            {
                ShowCloseIcon = true,
                Margin = new Thickness(0, 0, 6, 6),
            };
            chip.CloseClicked += (_, _) => RemoveTag(tagText);
            _wrap.Children.Add(chip);
        }
        _wrap.Children.Add(_entry);
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        var focused = _entry.IsFocused;

        _label.Apply(MavTypeRole.LabelMedium, t.Typography);
        _label.Foreground = MavTheme.BrushOf(focused ? c.Primary : c.OnSurfaceVariant);

        _entry.FontFamily = t.Typography.PlainFont;
        _entry.FontSize = 15;
        _entry.TextColor = c.OnSurface;
        _entry.PlaceholderColor = c.OnSurfaceVariant;

        _container.Radius = t.Shape.Medium;
        _container.FillColor = c.SurfaceContainerLow;
        _container.BorderStrokeColor = focused ? c.Primary : c.Outline;
        _container.BorderStrokeThickness = focused ? 2 : 1;
    }
}
