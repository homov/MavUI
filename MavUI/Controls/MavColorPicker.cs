using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.ColorUtilities.ColorAppearance;
using Mav.ColorUtilities.Palettes;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 HCT / Hex Color Picker with interactive Hue spectrum strip,
/// HCT Chroma &amp; Tone sliders, Hex code editor, live tonal preview, and Material You seed swatches.
/// </summary>
public class MavColorPicker : MavContentView, IMavDraggableControl
{
    public static readonly StyledProperty<Color> SelectedColorProperty =
        AvaloniaProperty.Register<MavColorPicker, Color>(
            nameof(SelectedColor),
            Color.FromRgb(0x67, 0x50, 0xA4),
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> ShowHexInputProperty =
        AvaloniaProperty.Register<MavColorPicker, bool>(nameof(ShowHexInput), true);

    public static readonly StyledProperty<bool> ShowHctSlidersProperty =
        AvaloniaProperty.Register<MavColorPicker, bool>(nameof(ShowHctSliders), true);

    public static readonly StyledProperty<bool> ShowPresetSwatchesProperty =
        AvaloniaProperty.Register<MavColorPicker, bool>(nameof(ShowPresetSwatches), true);

    private static readonly Color[] PresetSwatches =
    [
        MavColors.Purple,
        MavColors.Indigo,
        MavColors.Blue,
        MavColors.Teal,
        MavColors.Green,
        MavColors.Lime,
        MavColors.Amber,
        MavColors.Orange,
        MavColors.Red,
        MavColors.Pink,
        MavColors.Rose,
        MavColors.Slate,
    ];

    private readonly MavSurface _previewSwatch;
    private readonly TextBlock _hexBadgeLbl;
    private readonly TextBlock _hctBadgeLbl;
    private readonly HueSpectrumBar _hueBar;
    private readonly StackPanel _slidersPanel;
    private readonly MavSlider _hueSlider;
    private readonly MavSlider _chromaSlider;
    private readonly MavSlider _toneSlider;
    private readonly TextBlock _hueValLbl;
    private readonly TextBlock _chromaValLbl;
    private readonly TextBlock _toneValLbl;
    private readonly MavTextField _hexField;
    private readonly WrapPanel _swatchesPanel;
    private readonly List<(Color Preset, MavInkSurface Surface, MavIcon CheckIcon)> _swatchItems = [];

    private bool _syncing;
    private double _hue = 275;
    private double _chroma = 48;
    private double _tone = 40;

    public event EventHandler<Color>? ColorChanged;

    static MavColorPicker()
    {
        SelectedColorProperty.Changed.AddClassHandler<MavColorPicker>((cp, e) =>
        {
            var col = e.GetNewValue<Color>();
            cp.SyncFromColor(col);
            cp.ColorChanged?.Invoke(cp, col);
        });
        ShowHexInputProperty.Changed.AddClassHandler<MavColorPicker>((cp, e) =>
            cp._hexField.IsVisible = e.GetNewValue<bool>());
        ShowHctSlidersProperty.Changed.AddClassHandler<MavColorPicker>((cp, e) =>
            cp._slidersPanel.IsVisible = e.GetNewValue<bool>());
        ShowPresetSwatchesProperty.Changed.AddClassHandler<MavColorPicker>((cp, e) =>
            cp._swatchesPanel.IsVisible = e.GetNewValue<bool>());
    }

    /// <summary>Creates a color picker with the default color.</summary>
    public MavColorPicker() : this(null)
    {
    }

    /// <summary>Creates a color picker that starts with <paramref name="initialColor"/>.</summary>
    public MavColorPicker(Color? initialColor = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _hexBadgeLbl = new TextBlock { FontWeight = FontWeight.SemiBold };
        _hctBadgeLbl = new TextBlock();

        var previewInfoCol = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center,
        };
        previewInfoCol.Children.Add(_hexBadgeLbl);
        previewInfoCol.Children.Add(_hctBadgeLbl);

        _previewSwatch = new MavSurface
        {
            Height = 68,
            Radius = 16,
            Padding = new Thickness(16, 10),
            BorderStrokeThickness = 1,
            Child = previewInfoCol,
        };

        _hueBar = new HueSpectrumBar
        {
            Height = 36,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 4, 0, 2),
        };
        _hueBar.HueSelected += (_, h) =>
        {
            if (_syncing) return;
            _hue = h;
            SyncFromHct();
        };

        _hueValLbl = new TextBlock { Width = 38, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        _chromaValLbl = new TextBlock { Width = 38, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };
        _toneValLbl = new TextBlock { Width = 38, TextAlignment = TextAlignment.Right, VerticalAlignment = VerticalAlignment.Center };

        _hueSlider = new MavSlider(0, 360, 275);
        _hueSlider.ValueChanged += (_, v) =>
        {
            if (_syncing) return;
            _hue = v;
            SyncFromHct();
        };

        _chromaSlider = new MavSlider(0, 100, 48);
        _chromaSlider.ValueChanged += (_, v) =>
        {
            if (_syncing) return;
            _chroma = v;
            SyncFromHct();
        };

        _toneSlider = new MavSlider(5, 95, 40);
        _toneSlider.ValueChanged += (_, v) =>
        {
            if (_syncing) return;
            _tone = v;
            SyncFromHct();
        };

        _slidersPanel = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 4,
        };
        _slidersPanel.Children.Add(BuildSliderRow("H", _hueSlider, _hueValLbl));
        _slidersPanel.Children.Add(BuildSliderRow("C", _chromaSlider, _chromaValLbl));
        _slidersPanel.Children.Add(BuildSliderRow("T", _toneSlider, _toneValLbl));

        _hexField = new MavTextField("Hex color (#RRGGBB)", MavTextFieldVariant.Outlined)
        {
            LeadingIcon = MavIcons.Palette,
        };
        _hexField.TextChanged += (_, txt) =>
        {
            if (_syncing || string.IsNullOrWhiteSpace(txt)) return;
            var s = txt.Trim();
            if (!s.StartsWith('#')) s = "#" + s;
            if ((s.Length == 7 || s.Length == 9) && Color.TryParse(s, out var parsed))
            {
                _syncing = true;
                try
                {
                    SelectedColor = parsed;
                    UpdateHctFromColor(parsed);
                    UpdateVisuals();
                }
                finally
                {
                    _syncing = false;
                }
            }
        };

        _swatchesPanel = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
        };
        BuildSwatchesOnce();

        var root = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 12,
        };
        root.Children.Add(_previewSwatch);
        root.Children.Add(_hueBar);
        root.Children.Add(_slidersPanel);
        root.Children.Add(_hexField);
        root.Children.Add(_swatchesPanel);

        Child = root;

        var startColor = initialColor ?? Color.FromRgb(0x67, 0x50, 0xA4);
        SelectedColor = startColor;
        SyncFromColor(startColor);

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public Color SelectedColor
    {
        get => GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    public bool ShowHexInput
    {
        get => GetValue(ShowHexInputProperty);
        set => SetValue(ShowHexInputProperty, value);
    }

    public bool ShowHctSliders
    {
        get => GetValue(ShowHctSlidersProperty);
        set => SetValue(ShowHctSlidersProperty, value);
    }

    public bool ShowPresetSwatches
    {
        get => GetValue(ShowPresetSwatchesProperty);
        set => SetValue(ShowPresetSwatchesProperty, value);
    }

    /// <summary>Shows a color picker next to <paramref name="anchor"/> and returns the picked color, or <c>null</c> if it was cancelled.</summary>
    public static Task<Color?> PickAsync(Control anchor, Color? initialColor = null, string title = "Select color")
    {
        var tcs = new TaskCompletionSource<Color?>();

        // Match the adaptive dialog width: margins + 24 dp of card padding on each side.
        var overlaySize = MavOverlay.GetOverlaySize(anchor);
        var insets = MavOverlay.ResolveTopLevel(anchor)?.InsetsManager?.SafeAreaPadding ?? default;
        var maxDialogWidth = Math.Max(
            0,
            Math.Min(560, overlaySize.Width - insets.Left - insets.Right - 48));
        var maxPickerWidth = Math.Max(0, maxDialogWidth - 48);

        var picker = new MavColorPicker(initialColor ?? MavTheme.SeedColor)
        {
            MinWidth = Math.Min(280, maxPickerWidth),
        };

        var dlg = new MavDialog
        {
            Icon = MavIcons.Palette,
            Title = title,
            Body = picker,
            ConfirmText = MavLocalization.Select,
            DismissText = MavLocalization.Cancel,
        };
        dlg.Confirmed += (_, _) => tcs.TrySetResult(picker.SelectedColor);
        dlg.Dismissed += (_, _) => tcs.TrySetResult(null);
        dlg.Closed += (_, _) => tcs.TrySetResult(null);
        dlg.Show(anchor);
        return tcs.Task;
    }

    private static Control BuildSliderRow(string label, MavSlider slider, TextBlock valLbl)
    {
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("24,*,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
        };
        var lbl = new TextBlock
        {
            Text = label,
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        row.Add(lbl, 0, 0);
        row.Add(slider, 1, 0);
        row.Add(valLbl, 2, 0);
        return row;
    }

    private void SyncFromColor(Color color)
    {
        if (_syncing) return;
        _syncing = true;
        try
        {
            UpdateHctFromColor(color);
            _hexField.Text = FormatHex(color);
            UpdateVisuals();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void UpdateHctFromColor(Color color)
    {
        var hct = Hct.FromInt(MavColorScheme.ToArgb(color));
        _hue = hct.Hue;
        _chroma = Math.Clamp(hct.Chroma, 0, 100);
        _tone = Math.Clamp(hct.Tone, 5, 95);

        _hueSlider.Value = _hue;
        _chromaSlider.Value = _chroma;
        _toneSlider.Value = _tone;
        _hueBar.Hue = _hue;
    }

    private void SyncFromHct()
    {
        _syncing = true;
        try
        {
            var pal = TonalPalette.FromHueAndChroma(_hue, Math.Max(0, _chroma));
            var argb = pal.Tone((uint)Math.Clamp((int)Math.Round(_tone), 0, 100));
            var col = MavColorScheme.FromArgb(argb);

            _hueBar.Hue = _hue;
            _hueSlider.Value = _hue;
            _chromaSlider.Value = _chroma;
            _toneSlider.Value = _tone;
            _hexField.Text = FormatHex(col);

            SelectedColor = col;
            UpdateVisuals();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void UpdateVisuals()
    {
        var col = SelectedColor;
        _previewSwatch.FillColor = col;
        var lum = 0.2126 * col.R + 0.7152 * col.G + 0.0722 * col.B;
        var fg = lum > 145 ? Color.FromRgb(20, 20, 24) : Colors.White;

        _hexBadgeLbl.Text = FormatHex(col);
        _hexBadgeLbl.Foreground = MavTheme.BrushOf(fg);
        _hctBadgeLbl.Text = string.Create(
            CultureInfo.InvariantCulture,
            $"H {Math.Round(_hue):0}°  •  C {Math.Round(_chroma):0}  •  T {Math.Round(_tone):0}");
        _hctBadgeLbl.Foreground = MavTheme.BrushOf(fg.WithAlpha(0.82f));

        _hueValLbl.Text = $"{Math.Round(_hue):0}°";
        _chromaValLbl.Text = $"{Math.Round(_chroma):0}";
        _toneValLbl.Text = $"{Math.Round(_tone):0}";

        UpdateSwatchesSelection();
    }

    private void BuildSwatchesOnce()
    {
        _swatchesPanel.Children.Clear();
        _swatchItems.Clear();

        foreach (var preset in PresetSwatches)
        {
            var checkIcon = new MavIcon(MavIcons.Check, 16)
            {
                UseThemeDefaultColor = false,
                Color = Colors.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = false,
            };
            var swatch = new MavInkSurface
            {
                Width = 34,
                Height = 34,
                CornerRadius = 17,
                Margin = new Thickness(0, 0, 8, 8),
                FillColor = preset,
                StrokeThickness = 1,
                RippleColor = Colors.White,
                Child = checkIcon,
            };
            var captured = preset;
            swatch.Clicked += (_, _) => SelectedColor = captured;
            _swatchItems.Add((preset, swatch, checkIcon));
            _swatchesPanel.Children.Add(swatch);
        }
    }

    private void UpdateSwatchesSelection()
    {
        var c = MavTheme.Current.Color;
        var currentArgb = MavColorScheme.ToArgb(SelectedColor);

        foreach (var (preset, swatch, checkIcon) in _swatchItems)
        {
            var isSelected = MavColorScheme.ToArgb(preset) == currentArgb;
            swatch.StrokeThickness = isSelected ? 2.5 : 1;
            swatch.StrokeColor = isSelected ? c.OnSurface : c.OutlineVariant.WithAlpha(0.6f);
            checkIcon.IsVisible = isSelected;
        }
    }

    private void ApplyTheme(MavTheme t)
    {
        var c = t.Color;
        _previewSwatch.BorderStrokeColor = c.OutlineVariant;
        _hexBadgeLbl.Apply(MavTypeRole.TitleMedium, t.Typography);
        _hctBadgeLbl.Apply(MavTypeRole.LabelSmall, t.Typography);
        _hueValLbl.Apply(MavTypeRole.LabelSmall, t.Typography);
        _chromaValLbl.Apply(MavTypeRole.LabelSmall, t.Typography);
        _toneValLbl.Apply(MavTypeRole.LabelSmall, t.Typography);

        _hueValLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        _chromaValLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);
        _toneValLbl.Foreground = MavTheme.BrushOf(c.OnSurfaceVariant);

        UpdateVisuals();
    }

    private static string FormatHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    private sealed class HueSpectrumBar : Control
    {
        private const double ThumbRadius = 11.0;
        private static readonly LinearGradientBrush SpectrumBrush = CreateSpectrumBrush();

        private double _hue;
        private bool _dragging;

        public event EventHandler<double>? HueSelected;

        public double Hue
        {
            get => _hue;
            set
            {
                _hue = Math.Clamp(value, 0, 360);
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Creates the hue bar that lets the user pick a hue.
        /// </summary>
        public HueSpectrumBar()
        {
            Height = 36;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            ClipToBounds = false;
            Focusable = true;
            Cursor = new Cursor(StandardCursorType.Hand);
        }

        /// <summary>
        /// Measures the hue bar during the layout pass.
        /// </summary>
        protected override Size MeasureOverride(Size availableSize)
        {
            var w = double.IsFinite(availableSize.Width) && availableSize.Width > 0 ? availableSize.Width : 240;
            return new Size(w, 36);
        }

        /// <summary>
        /// Starts a hue drag and captures the pointer.
        /// </summary>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (!IsEnabled) return;
            if (MavGestureZones.ShouldDeferToSystem(this, e)) return;
            _dragging = true;
            e.Pointer.Capture(this);
            e.PreventGestureRecognition();
            UpdateFromX(e.GetPosition(this).X);
            e.Handled = true;
        }

        /// <summary>
        /// Updates the hue while the pointer is dragged.
        /// </summary>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (!_dragging || !IsEnabled) return;
            e.PreventGestureRecognition();
            UpdateFromX(e.GetPosition(this).X);
            e.Handled = true;
        }

        /// <summary>
        /// Ends the hue drag when the pointer is released.
        /// </summary>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (!_dragging) return;
            _dragging = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        /// <summary>
        /// Ends the hue drag when the pointer capture is lost.
        /// </summary>
        protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
        {
            base.OnPointerCaptureLost(e);
            _dragging = false;
        }

        private void UpdateFromX(double x)
        {
            var w = Bounds.Width;
            var usable = Math.Max(1.0, w - ThumbRadius * 2.0);
            var ratio = Math.Clamp((x - ThumbRadius) / usable, 0.0, 1.0);
            Hue = ratio * 360.0;
            HueSelected?.Invoke(this, _hue);
        }

        /// <summary>
        /// Draws the hue gradient and the selection indicator.
        /// </summary>
        public override void Render(DrawingContext context)
        {
            var w = Bounds.Width;
            var h = Bounds.Height;
            if (w <= 0 || h <= 0) return;

            // Transparent hit-testable background over the full 36dp height
            context.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, w, h));

            var trackRect = new Rect(0, (h - 16) / 2, w, 16);
            context.DrawRectangle(SpectrumBrush, null, new RoundedRect(trackRect, 8));

            var usable = Math.Max(1.0, w - ThumbRadius * 2.0);
            var thumbX = ThumbRadius + (_hue / 360.0) * usable;
            var thumbCenter = new Point(thumbX, h / 2);
            var activeColor = HctStop(_hue);

            context.DrawEllipse(
                Brushes.White,
                MavTheme.PenOf(MavTheme.Current.Color.Outline.WithAlpha(0.6f), 1.5),
                thumbCenter,
                ThumbRadius,
                ThumbRadius);
            context.DrawEllipse(
                MavTheme.BrushOf(activeColor),
                null,
                thumbCenter,
                7.5,
                7.5);
        }

        private static LinearGradientBrush CreateSpectrumBrush()
        {
            var stops = new GradientStops
            {
                new GradientStop(HctStop(0), 0.00),
                new GradientStop(HctStop(60), 0.17),
                new GradientStop(HctStop(120), 0.33),
                new GradientStop(HctStop(180), 0.50),
                new GradientStop(HctStop(240), 0.67),
                new GradientStop(HctStop(300), 0.83),
                new GradientStop(HctStop(360), 1.00),
            };
            return new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0.5, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 0.5, RelativeUnit.Relative),
                GradientStops = stops,
            };
        }

        private static Color HctStop(double hue)
        {
            var argb = TonalPalette.FromHueAndChroma(hue % 360.0, 64).Tone(55);
            return MavColorScheme.FromArgb(argb);
        }
    }
}
