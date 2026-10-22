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
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Numeric Stepper Input with decrement/increment buttons, min/max clamping,
/// step increments, keyboard Up/Down support, and optional label.
/// </summary>
public class MdNumberBox : MdContentView
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MdNumberBox, double>(
            nameof(Value),
            0.0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MdNumberBox, double>(nameof(Minimum), double.MinValue);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MdNumberBox, double>(nameof(Maximum), double.MaxValue);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MdNumberBox, double>(nameof(Step), 1.0);

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<MdNumberBox, int>(nameof(DecimalPlaces), 0);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MdNumberBox, string?>(nameof(Label));

    public static readonly StyledProperty<string?> SuffixProperty =
        AvaloniaProperty.Register<MdNumberBox, string?>(nameof(Suffix));

    public static readonly StyledProperty<string?> FormatStringProperty =
        AvaloniaProperty.Register<MdNumberBox, string?>(nameof(FormatString));

    private readonly MdSurface _box;
    private readonly TextBlock _label;
    private readonly MdIconButton _decBtn;
    private readonly MdNativeEntry _entry;
    private readonly TextBlock _suffixLbl;
    private readonly MdIconButton _incBtn;
    private bool _updating;

    public event EventHandler<double>? ValueChanged;

    static MdNumberBox()
    {
        ValueProperty.Changed.AddClassHandler<MdNumberBox>((n, e) => n.OnValuePropertyChanged(e.GetNewValue<double>()));
        MinimumProperty.Changed.AddClassHandler<MdNumberBox>((n, _) => n.SyncButtons());
        MaximumProperty.Changed.AddClassHandler<MdNumberBox>((n, _) => n.SyncButtons());
        DecimalPlacesProperty.Changed.AddClassHandler<MdNumberBox>((n, _) => n.FormatEntry());
        FormatStringProperty.Changed.AddClassHandler<MdNumberBox>((n, _) => n.FormatEntry());
        LabelProperty.Changed.AddClassHandler<MdNumberBox>((n, e) =>
        {
            var txt = e.GetNewValue<string?>();
            n._label.Text = txt ?? string.Empty;
            n._label.IsVisible = !string.IsNullOrEmpty(txt);
            MdAutomation.SetName(n, txt);
        });
        SuffixProperty.Changed.AddClassHandler<MdNumberBox>((n, e) =>
        {
            var txt = e.GetNewValue<string?>();
            n._suffixLbl.Text = txt ?? string.Empty;
            n._suffixLbl.IsVisible = !string.IsNullOrEmpty(txt);
        });
    }

    public MdNumberBox() : this(0, 0, 100, 1, 0, null, null)
    {
    }

    public MdNumberBox(
        double value = 0,
        double min = 0,
        double max = 100,
        double step = 1,
        int decimalPlaces = 0,
        string? label = null,
        string? suffix = null)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;

        _label = new TextBlock
        {
            Text = label ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(label),
            Margin = new Thickness(4, 0, 4, 4),
        };

        _decBtn = new MdIconButton(MdIcons.Remove, MdIconButtonVariant.Standard)
        {
            ButtonSize = 36,
        };
        _decBtn.Clicked += (_, _) => Decrement();

        _entry = new MdNativeEntry
        {
            MinWidth = 64,
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        TextInputOptions.SetContentType(_entry, TextInputContentType.Number);
        TextInputOptions.SetReturnKeyType(_entry, TextInputReturnKeyType.Done);
        _entry.LostFocus += (_, _) => CommitEntryText();
        _entry.Completed += (_, _) => CommitEntryText();
        _entry.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Up)
            {
                Increment();
                e.Handled = true;
            }
            else if (e.Key == Key.Down)
            {
                Decrement();
                e.Handled = true;
            }
        };

        _suffixLbl = new TextBlock
        {
            Text = suffix ?? string.Empty,
            IsVisible = !string.IsNullOrEmpty(suffix),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 4, 0),
        };

        _incBtn = new MdIconButton(MdIcons.Add, MdIconButtonVariant.Standard)
        {
            ButtonSize = 36,
        };
        _incBtn.Clicked += (_, _) => Increment();

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        row.Add(_decBtn, 0, 0);
        row.Add(_entry, 1, 0);
        row.Add(_suffixLbl, 2, 0);
        row.Add(_incBtn, 3, 0);

        _box = new MdSurface
        {
            MinHeight = 48,
            MinWidth = 156,
            Radius = 12,
            Padding = new Thickness(4, 2),
            BorderStrokeThickness = 1,
            Content = row,
        };

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
        };
        root.Children.Add(_label);
        root.Children.Add(_box);

        Content = root;

        Minimum = min;
        Maximum = max;
        Step = step;
        DecimalPlaces = decimalPlaces;
        if (label is not null) Label = label;
        if (suffix is not null) Suffix = suffix;
        Value = Math.Clamp(value, min, max);
        FormatEntry();
        SyncButtons();
        MdAutomation.SetName(this, Label);
        MdAutomation.SetControlType(this, AutomationControlType.Edit);

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Math.Clamp(value, Minimum, Maximum));
    }

    public double Minimum
    {
        get => GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public double Step
    {
        get => GetValue(StepProperty);
        set => SetValue(StepProperty, Math.Max(1e-6, value));
    }

    public int DecimalPlaces
    {
        get => GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, Math.Clamp(value, 0, 6));
    }

    public string? Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? Suffix
    {
        get => GetValue(SuffixProperty);
        set => SetValue(SuffixProperty, value);
    }

    public string? FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        if (!IsEnabled || !_entry.IsFocused) return;
        if (e.Delta.Y > 0)
        {
            Increment();
            e.Handled = true;
        }
        else if (e.Delta.Y < 0)
        {
            Decrement();
            e.Handled = true;
        }
    }

    public void Increment() => Value = Math.Clamp(Math.Round((Value + Step) / Step) * Step, Minimum, Maximum);
    public void Decrement() => Value = Math.Clamp(Math.Round((Value - Step) / Step) * Step, Minimum, Maximum);

    private void CommitEntryText()
    {
        if (double.TryParse(_entry.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
        {
            Value = Math.Clamp(parsed, Minimum, Maximum);
        }
        FormatEntry();
    }

    private void OnValuePropertyChanged(double val)
    {
        if (_updating) return;
        FormatEntry();
        SyncButtons();
        ValueChanged?.Invoke(this, val);
    }

    private void FormatEntry()
    {
        _updating = true;
        var fmt = !string.IsNullOrWhiteSpace(FormatString)
            ? FormatString!
            : (DecimalPlaces > 0 ? $"F{DecimalPlaces}" : "0");
        _entry.Text = Value.ToString(fmt, CultureInfo.InvariantCulture);
        _updating = false;
    }

    private void SyncButtons()
    {
        _decBtn.IsEnabled = IsEnabled && Value > Minimum + 1e-9;
        _incBtn.IsEnabled = IsEnabled && Value < Maximum - 1e-9;
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _label.Apply(MdTypeRole.LabelMedium, t.Typography);
        _label.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);

        _suffixLbl.Apply(MdTypeRole.BodyMedium, t.Typography);
        _suffixLbl.Foreground = MdTheme.BrushOf(c.OnSurfaceVariant);

        _entry.FontFamily = t.Typography.PlainFont;
        _entry.FontSize = 16;
        _entry.TextColor = c.OnSurface;

        _box.Radius = t.Shape.Medium;
        _box.FillColor = c.SurfaceContainerLow;
        _box.BorderStrokeColor = c.OutlineVariant;
        SyncButtons();
    }
}

/// <summary>
/// Material Design 3 Interactive Rating Bar supporting full or half-step precision,
/// pointer drag &amp; click, and keyboard left/right navigation.
/// </summary>
public class MdRatingBar : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MdRatingBar, double>(
            nameof(Value),
            3.5,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxRatingProperty =
        AvaloniaProperty.Register<MdRatingBar, int>(nameof(MaxRating), 5);

    public static readonly StyledProperty<bool> AllowHalfProperty =
        AvaloniaProperty.Register<MdRatingBar, bool>(nameof(AllowHalf), true);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<MdRatingBar, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<MdRatingBar, double>(nameof(IconSize), 28.0);

    private bool _dragging;

    public event EventHandler<double>? ValueChanged;

    static MdRatingBar()
    {
        ValueProperty.Changed.AddClassHandler<MdRatingBar>((r, e) =>
        {
            r.InvalidateVisual();
            r.ValueChanged?.Invoke(r, e.GetNewValue<double>());
        });
        MaxRatingProperty.Changed.AddClassHandler<MdRatingBar>((r, _) =>
        {
            r.InvalidateMeasure();
            r.InvalidateVisual();
        });
        AllowHalfProperty.Changed.AddClassHandler<MdRatingBar>((r, _) => r.InvalidateVisual());
        IconSizeProperty.Changed.AddClassHandler<MdRatingBar>((r, _) =>
        {
            r.InvalidateMeasure();
            r.InvalidateVisual();
        });
    }

    public MdRatingBar() : this(4.0, 5, true, 28)
    {
    }

    public MdRatingBar(double value = 4.0, int maxRating = 5, bool allowHalf = true, double iconSize = 28)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);

        MaxRating = Math.Clamp(maxRating, 1, 10);
        AllowHalf = allowHalf;
        IconSize = iconSize;
        Value = Math.Clamp(value, 0, MaxRating);

        MdTheme.Observe(this, (self, _) => self.InvalidateVisual());
    }

    public double Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, Snap(value));
    }

    public int MaxRating
    {
        get => GetValue(MaxRatingProperty);
        set => SetValue(MaxRatingProperty, Math.Clamp(value, 1, 10));
    }

    public bool AllowHalf
    {
        get => GetValue(AllowHalfProperty);
        set => SetValue(AllowHalfProperty, value);
    }

    public bool AllowHalfStars
    {
        get => AllowHalf;
        set => AllowHalf = value;
    }

    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    public double IconSize
    {
        get => GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, Math.Max(16, value));
    }

    public double ItemSize
    {
        get => IconSize;
        set => IconSize = value;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        const double gap = 4.0;
        var w = MaxRating * IconSize + Math.Max(0, MaxRating - 1) * gap;
        return new Size(w, IconSize + 6);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled || IsReadOnly) return;
        _dragging = true;
        e.Pointer.Capture(this);
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || !IsEnabled || IsReadOnly) return;
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!IsEnabled || IsReadOnly) return;
        var step = AllowHalf ? 0.5 : 1.0;
        if (e.Key is Key.Left or Key.Down)
        {
            Value = Math.Max(0, Value - step);
            e.Handled = true;
        }
        else if (e.Key is Key.Right or Key.Up)
        {
            Value = Math.Min(MaxRating, Value + step);
            e.Handled = true;
        }
    }

    private void UpdateFromX(double x)
    {
        var totalW = Math.Max(1.0, Bounds.Width);
        var raw = (x / totalW) * MaxRating;
        Value = Snap(raw);
    }

    private double Snap(double raw)
    {
        var clamped = Math.Clamp(raw, 0, MaxRating);
        return AllowHalf
            ? Math.Round(clamped * 2.0) / 2.0
            : Math.Round(clamped);
    }

    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var c = MdTheme.Current.Color;
        var activeBrush = MdTheme.BrushOf(IsEnabled ? c.Primary : c.OnSurface.WithAlpha(0.38f));
        var emptyBrush = MdTheme.BrushOf(IsEnabled ? c.OutlineVariant : c.OnSurface.WithAlpha(0.18f));
        var typeface = MdFonts.MaterialIconsTypeface;

        const double gap = 4.0;
        var sz = IconSize;
        var cy = h / 2.0;

        for (int i = 0; i < MaxRating; i++)
        {
            var x = i * (sz + gap);
            var diff = Value - i;
            string glyph;
            IBrush brush;

            if (diff >= 0.75)
            {
                glyph = MdIcons.Star;
                brush = activeBrush;
            }
            else if (diff >= 0.25 && AllowHalf)
            {
                glyph = MdIcons.StarHalf;
                brush = activeBrush;
            }
            else
            {
                glyph = MdIcons.StarBorder;
                brush = emptyBrush;
            }

            var ft = new FormattedText(
                glyph,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                sz,
                brush);
            context.DrawText(ft, new Point(x + (sz - ft.Width) / 2.0, cy - ft.Height / 2.0));
        }
    }
}

/// <summary>
/// Material Design 3 Tag / Chip Input control that converts typed entries into removable
/// <see cref="MdChip"/> items when pressing Enter or comma.
/// </summary>
public class MdTagInput : MdContentView, IEnumerable<string>
{
    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MdTagInput, string?>(nameof(Label));

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<MdTagInput, string>(nameof(Placeholder), "Add tag…");

    public static readonly StyledProperty<int> MaxTagsProperty =
        AvaloniaProperty.Register<MdTagInput, int>(nameof(MaxTags), 0);

    public static readonly StyledProperty<bool> AllowDuplicatesProperty =
        AvaloniaProperty.Register<MdTagInput, bool>(nameof(AllowDuplicates));

    public static readonly StyledProperty<System.Collections.IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<MdTagInput, System.Collections.IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<ICommand?> TagAddedCommandProperty =
        AvaloniaProperty.Register<MdTagInput, ICommand?>(nameof(TagAddedCommand));

    public static readonly StyledProperty<ICommand?> TagRemovedCommandProperty =
        AvaloniaProperty.Register<MdTagInput, ICommand?>(nameof(TagRemovedCommand));

    private readonly AvaloniaList<string> _tags = new();
    private readonly MdCollectionObserver _observer;
    private readonly MdSurface _container;
    private readonly TextBlock _label;
    private readonly WrapPanel _wrap;
    private readonly MdNativeEntry _entry;
    private bool _syncingTags;

    public event EventHandler<string>? TagAdded;
    public event EventHandler<string>? TagRemoved;
    public event EventHandler? TagsChanged;

    static MdTagInput()
    {
        LabelProperty.Changed.AddClassHandler<MdTagInput>((t, e) =>
        {
            var txt = e.GetNewValue<string?>();
            t._label.Text = txt ?? string.Empty;
            t._label.IsVisible = !string.IsNullOrEmpty(txt);
        });
        PlaceholderProperty.Changed.AddClassHandler<MdTagInput>((t, e) =>
            t._entry.Placeholder = e.GetNewValue<string>());
        ItemsSourceProperty.Changed.AddClassHandler<MdTagInput>((t, e) =>
            t.OnItemsSourceChanged(e.GetNewValue<System.Collections.IEnumerable?>()));
    }

    public MdTagInput() : this(null, "Add tag…", null)
    {
    }

    public MdTagInput(
        string? label = null,
        string placeholder = "Add tag…",
        IEnumerable<string>? initialTags = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        _observer = new MdCollectionObserver(() => ReloadFromItemsSource(ItemsSource));
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

        _entry = new MdNativeEntry
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
                ApplyTheme(MdTheme.Current);
            }
        };

        _wrap = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center,
        };
        _wrap.Children.Add(_entry);

        _container = new MdSurface
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

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public MdTagInput AddTag(string tag)
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

    public void Add(string tag) => AddTag(tag);

    public IEnumerator<string> GetEnumerator() => _tags.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

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
            var chip = new MdChip(tagText, MdChipKind.Input)
            {
                ShowCloseIcon = true,
                Margin = new Thickness(0, 0, 6, 6),
            };
            chip.CloseClicked += (_, _) => RemoveTag(tagText);
            _wrap.Children.Add(chip);
        }
        _wrap.Children.Add(_entry);
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        var focused = _entry.IsFocused;

        _label.Apply(MdTypeRole.LabelMedium, t.Typography);
        _label.Foreground = MdTheme.BrushOf(focused ? c.Primary : c.OnSurfaceVariant);

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
