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
