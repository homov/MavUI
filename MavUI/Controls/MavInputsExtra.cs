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
/// Material Design 3 Numeric Stepper Input with decrement/increment buttons, min/max clamping,
/// step increments, keyboard Up/Down support, and optional label.
/// </summary>
public class MavNumberBox : MavContentView
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(
            nameof(Value),
            0.0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<double> MinimumProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(nameof(Minimum), double.MinValue);

    public static readonly StyledProperty<double> MaximumProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(nameof(Maximum), double.MaxValue);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<MavNumberBox, double>(nameof(Step), 1.0);

    public static readonly StyledProperty<int> DecimalPlacesProperty =
        AvaloniaProperty.Register<MavNumberBox, int>(nameof(DecimalPlaces), 0);

    public static readonly StyledProperty<string?> LabelProperty =
        AvaloniaProperty.Register<MavNumberBox, string?>(nameof(Label));

    public static readonly StyledProperty<string?> SuffixProperty =
        AvaloniaProperty.Register<MavNumberBox, string?>(nameof(Suffix));

    public static readonly StyledProperty<string?> FormatStringProperty =
        AvaloniaProperty.Register<MavNumberBox, string?>(nameof(FormatString));

    private readonly MavSurface _box;
    private readonly TextBlock _label;
    private readonly MavIconButton _decBtn;
    private readonly MavNativeEntry _entry;
    private readonly TextBlock _suffixLbl;
    private readonly MavIconButton _incBtn;
    private bool _updating;

    public event EventHandler<double>? ValueChanged;

    static MavNumberBox()
    {
        ValueProperty.Changed.AddClassHandler<MavNumberBox>((n, e) => n.OnValuePropertyChanged(e.GetNewValue<double>()));
        MinimumProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.SyncButtons());
        MaximumProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.SyncButtons());
        DecimalPlacesProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.FormatEntry());
        FormatStringProperty.Changed.AddClassHandler<MavNumberBox>((n, _) => n.FormatEntry());
        LabelProperty.Changed.AddClassHandler<MavNumberBox>((n, e) =>
        {
            var txt = e.GetNewValue<string?>();
            n._label.Text = txt ?? string.Empty;
            n._label.IsVisible = !string.IsNullOrEmpty(txt);
            MavAutomation.SetName(n, txt);
        });
        SuffixProperty.Changed.AddClassHandler<MavNumberBox>((n, e) =>
        {
            var txt = e.GetNewValue<string?>();
            n._suffixLbl.Text = txt ?? string.Empty;
            n._suffixLbl.IsVisible = !string.IsNullOrEmpty(txt);
        });
    }

    /// <summary>Creates a number box with the range 0 to 100 and step 1.</summary>
    public MavNumberBox() : this(0, 0, 100, 1, 0, null, null)
    {
    }

    /// <summary>Creates a number box with the given value range, step and formatting.</summary>
    public MavNumberBox(
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

        _decBtn = new MavIconButton(MavIcons.Remove, MavIconButtonVariant.Standard)
        {
            ButtonSize = 36,
        };
        _decBtn.Clicked += (_, _) => Decrement();

        _entry = new MavNativeEntry
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

        _incBtn = new MavIconButton(MavIcons.Add, MavIconButtonVariant.Standard)
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

        _box = new MavSurface
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
        MavAutomation.SetName(this, Label);
        MavAutomation.SetControlType(this, AutomationControlType.Edit);

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    /// <summary>Changes the value by one step per wheel notch when the box has focus.</summary>
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

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _label.Apply(MavTypeRole.LabelMedium, t.Typography);
        _label.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        _suffixLbl.Apply(MavTypeRole.BodyMedium, t.Typography);
        _suffixLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

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
public class MavRatingBar : Control
{
    public static readonly StyledProperty<double> ValueProperty =
        AvaloniaProperty.Register<MavRatingBar, double>(
            nameof(Value),
            3.5,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxRatingProperty =
        AvaloniaProperty.Register<MavRatingBar, int>(nameof(MaxRating), 5);

    public static readonly StyledProperty<bool> AllowHalfProperty =
        AvaloniaProperty.Register<MavRatingBar, bool>(nameof(AllowHalf), true);

    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<MavRatingBar, bool>(nameof(IsReadOnly));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<MavRatingBar, double>(nameof(IconSize), 28.0);

    private bool _dragging;

    public event EventHandler<double>? ValueChanged;

    static MavRatingBar()
    {
        ValueProperty.Changed.AddClassHandler<MavRatingBar>((r, e) =>
        {
            r.InvalidateVisual();
            r.ValueChanged?.Invoke(r, e.GetNewValue<double>());
        });
        MaxRatingProperty.Changed.AddClassHandler<MavRatingBar>((r, _) =>
        {
            r.InvalidateMeasure();
            r.InvalidateVisual();
        });
        AllowHalfProperty.Changed.AddClassHandler<MavRatingBar>((r, _) => r.InvalidateVisual());
        IconSizeProperty.Changed.AddClassHandler<MavRatingBar>((r, _) =>
        {
            r.InvalidateMeasure();
            r.InvalidateVisual();
        });
    }

    /// <summary>Creates a five-star rating bar with a value of 4.</summary>
    public MavRatingBar() : this(4.0, 5, true, 28)
    {
    }

    /// <summary>Creates a rating bar with the given value, maximum rating and half-star support.</summary>
    public MavRatingBar(double value = 4.0, int maxRating = 5, bool allowHalf = true, double iconSize = 28)
    {
        HorizontalAlignment = HorizontalAlignment.Left;
        VerticalAlignment = VerticalAlignment.Center;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Hand);

        MaxRating = Math.Clamp(maxRating, 1, 10);
        AllowHalf = allowHalf;
        IconSize = iconSize;
        Value = Math.Clamp(value, 0, MaxRating);

        MavTheme.Observe(this, (self, _) => self.InvalidateVisual());
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

    /// <summary>Measures the stars so the bar keeps a constant star size.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        const double gap = 4.0;
        var w = MaxRating * IconSize + Math.Max(0, MaxRating - 1) * gap;
        return new Size(w, IconSize + 6);
    }

    /// <summary>Sets the rating from the star under the pointer and starts a drag.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!IsEnabled || IsReadOnly) return;
        _dragging = true;
        e.Pointer.Capture(this);
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    /// <summary>Updates the rating while the pointer drags across the stars.</summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_dragging || !IsEnabled || IsReadOnly) return;
        UpdateFromX(e.GetPosition(this).X);
        e.Handled = true;
    }

    /// <summary>Ends the drag and commits the rating.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_dragging) return;
        _dragging = false;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    /// <summary>Changes the rating with the arrow keys.</summary>
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

    /// <summary>Draws the stars, full, half and empty, for the current value.</summary>
    public override void Render(DrawingContext context)
    {
        var w = Bounds.Width;
        var h = Bounds.Height;
        if (w <= 0 || h <= 0) return;

        context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

        var c = MavTheme.Current.Color;
        var activeColor = IsEnabled ? c.Primary : c.OnSurface.WithAlpha(0.38f);
        var emptyColor = IsEnabled ? c.OutlineVariant : c.OnSurface.WithAlpha(0.18f);
        var typeface = MavFonts.MaterialIconsTypeface;

        const double gap = 4.0;
        var sz = IconSize;
        var cy = h / 2.0;

        for (int i = 0; i < MaxRating; i++)
        {
            var x = i * (sz + gap);
            var diff = Value - i;
            string glyph;
            Color color;

            if (diff >= 0.75)
            {
                glyph = MavIcons.Star;
                color = activeColor;
            }
            else if (diff >= 0.25 && AllowHalf)
            {
                glyph = MavIcons.StarHalf;
                color = activeColor;
            }
            else
            {
                glyph = MavIcons.StarBorder;
                color = emptyColor;
            }

            var ft = MavTextMetrics.GetFormattedText(glyph, sz, typeface, color);
            context.DrawText(ft, new Point(x + (sz - ft.Width) / 2.0, cy - ft.Height / 2.0));
        }
    }
}

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

    public void Add(string tag) => AddTag(tag);

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
