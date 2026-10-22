using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Md3.Avalonia.Platform;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Primitives
{
    /// <summary>
    /// Shared drawing helpers for Material Design 3 arc paths and text field chrome (Filled / Outlined with notch cutout).
    /// </summary>
    internal static class MdDrawingHelpers
    {
        public static void DrawArc(
            DrawingContext ctx, IPen pen, double cx, double cy, double r, double startDeg, double sweepDeg)
        {
            if (r <= 0 || Math.Abs(sweepDeg) < 0.5) return;
            sweepDeg = Math.Clamp(sweepDeg, -359.9, 359.9);
            var startRad = startDeg * Math.PI / 180.0;
            var endRad = (startDeg + sweepDeg) * Math.PI / 180.0;
            var startPt = new Point(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad));
            var endPt = new Point(cx + r * Math.Cos(endRad), cy + r * Math.Sin(endRad));
            var isLargeArc = Math.Abs(sweepDeg) > 180.0;
            var dir = sweepDeg >= 0 ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;

            var geo = new StreamGeometry();
            using (var gc = geo.Open())
            {
                gc.BeginFigure(startPt, false);
                gc.ArcTo(endPt, new Size(r, r), 0, isLargeArc, dir);
                gc.EndFigure(false);
            }
            ctx.DrawGeometry(null, pen, geo);
        }

        public static void DrawFieldChrome(
            DrawingContext context,
            double w,
            double h,
            bool isFilled,
            bool isEnabled,
            bool hasError,
            bool isFocused,
            string? label,
            double floatProgress)
        {
            if (w <= 0 || h <= 0) return;

            var c = MdTheme.Current.Color;
            var strokeColor = !isEnabled
                ? c.OnSurface.WithAlpha(0.12f)
                : hasError
                    ? c.Error
                    : isFocused
                        ? c.Primary
                        : c.Outline;
            var strokeW = isFocused ? 2.0 : 1.0;
            var pen = MdTheme.PenOf(strokeColor, strokeW);

            if (isFilled)
            {
                var fill = !isEnabled
                    ? c.OnSurface.WithAlpha(0.04f)
                    : c.SurfaceContainerHighest;
                var rrect = new RoundedRect(
                    new Rect(0, 0, w, h),
                    new Vector(4, 4),
                    new Vector(4, 4),
                    new Vector(0, 0),
                    new Vector(0, 0));
                context.DrawRectangle(MdTheme.BrushOf(fill), null, rrect);
                context.DrawLine(
                    pen,
                    new Point(0, h - strokeW / 2.0),
                    new Point(w, h - strokeW / 2.0));
                return;
            }

            var inset = strokeW / 2.0;
            var left = inset;
            var top = inset;
            var right = w - inset;
            var bottom = h - inset;
            const double r = 4.0;

            var hasNotch = !string.IsNullOrEmpty(label) && floatProgress > 0.1;
            var notchWidth = hasNotch
                ? MdTextMetrics.MeasureWidth(label, 12, bold: true) * floatProgress + 8.0
                : 0.0;
            var notchStart = 12.0;
            var notchEnd = Math.Min(right - r - 4, notchStart + notchWidth);

            var geom = new StreamGeometry();
            using (var ctx = geom.Open())
            {
                if (hasNotch && notchEnd > notchStart + 2)
                {
                    ctx.BeginFigure(new Point(notchEnd, top), isFilled: false);
                    ctx.LineTo(new Point(right - r, top));
                    ctx.ArcTo(new Point(right, top + r), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(right, bottom - r));
                    ctx.ArcTo(new Point(right - r, bottom), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(left + r, bottom));
                    ctx.ArcTo(new Point(left, bottom - r), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(left, top + r));
                    ctx.ArcTo(new Point(left + r, top), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(notchStart, top));
                    ctx.EndFigure(isClosed: false);
                }
                else
                {
                    ctx.BeginFigure(new Point(left + r, top), isFilled: false);
                    ctx.LineTo(new Point(right - r, top));
                    ctx.ArcTo(new Point(right, top + r), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(right, bottom - r));
                    ctx.ArcTo(new Point(right - r, bottom), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(left + r, bottom));
                    ctx.ArcTo(new Point(left, bottom - r), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.LineTo(new Point(left, top + r));
                    ctx.ArcTo(new Point(left + r, top), new Size(r, r), 0, false, SweepDirection.Clockwise);
                    ctx.EndFigure(isClosed: true);
                }
            }

            context.DrawGeometry(null, pen, geom);
        }
    }
    /// <summary>
    /// Marker interface implemented by interactive drag controls (sliders, gauges, dials, color pickers, carousels, swipe items)
    /// so parent gesture hosts such as <see cref="Controls.MdRefreshIndicator"/> do not steal pointer capture.
    /// </summary>
    public interface IMdDraggableControl
    {
    }

    /// <summary>
    /// Shared visual-tree and collection utilities used across MD3 controls.
    /// </summary>
    internal static class MdVisualTreeHelpers
    {
        public static void DetachFromParent(Control? control)
        {
            if (control is null) return;
            switch (control.Parent)
            {
                case Panel p:
                    p.Children.Remove(control);
                    break;
                case Decorator d when ReferenceEquals(d.Child, control):
                    d.Child = null;
                    break;
                case ContentControl cc when ReferenceEquals(cc.Content, control):
                    cc.Content = null;
                    break;
            }
            if (control.Parent is not null && control is ISetLogicalParent slp)
            {
                slp.SetParent(null);
            }
        }

        public static void AttachLogicalChild(StyledElement parent, StyledElement? child)
        {
            if (child is null || ReferenceEquals(child.Parent, parent)) return;
            if (child.Parent is null && child is ISetLogicalParent slp)
                slp.SetParent(parent);
        }

        public static Control? BuildTemplateControl(
            object? item,
            IDataTemplate? dataTemplate,
            Func<object, Control>? factory = null)
        {
            if (item is null) return null;
            if (item is Control existingCtrl)
            {
                DetachFromParent(existingCtrl);
                return existingCtrl;
            }
            if (dataTemplate is not null && dataTemplate.Build(item) is { } built)
            {
                DetachFromParent(built);
                built.DataContext = item;
                return built;
            }
            if (factory is not null && factory(item) is { } fromFactory)
            {
                DetachFromParent(fromFactory);
                fromFactory.DataContext ??= item;
                return fromFactory;
            }
            return null;
        }

        [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(
            "Trimming",
            "IL2072",
            Justification = "DisplayMemberPath targets an application model type; trimmed apps " +
                            "must root those properties or use ItemTemplate/ItemFormatter.")]
        public static string FormatItemText(
            object? item,
            string? displayMemberPath,
            Func<object, string>? formatter = null)
        {
            if (item is null) return string.Empty;
            if (formatter is not null) return formatter(item);
            if (!string.IsNullOrWhiteSpace(displayMemberPath))
            {
                var prop = MdPropertyCache.Get(item.GetType(), displayMemberPath);
                if (prop is not null)
                    return prop.GetValue(item)?.ToString() ?? string.Empty;
            }
            return item.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Manages <see cref="INotifyCollectionChanged"/> subscriptions for <c>ItemsSource</c> properties.
    /// </summary>
    internal sealed class MdCollectionObserver
    {
        private INotifyCollectionChanged? _current;
        private readonly NotifyCollectionChangedEventHandler _handler;

        public MdCollectionObserver(Action onChanged)
        {
            _handler = (_, _) => onChanged();
        }

        public void Attach(IEnumerable? source)
        {
            if (_current is not null)
            {
                _current.CollectionChanged -= _handler;
                _current = null;
            }

            if (source is INotifyCollectionChanged incc)
            {
                _current = incc;
                _current.CollectionChanged += _handler;
            }
        }
    }
    /// <summary>
    /// Lightweight single-child container base class (Avalonia counterpart of MAUI ContentView)
    /// backed directly by <see cref="Border"/> with zero template overhead.
    /// </summary>
    public class MdContentView : Border
    {
        public static readonly StyledProperty<Control?> ContentProperty =
            ChildProperty.AddOwner<MdContentView>();

        public Control? Content
        {
            get => Child;
            set => Child = value;
        }

        public Color BackgroundColor
        {
            get => (Background as ISolidColorBrush)?.Color ?? Colors.Transparent;
            set => Background = value.A == 0 ? null : MdTheme.BrushOf(value);
        }
    }

    /// <summary>
    /// Rounded container with configurable corner radii, border stroke, tonal surface tint,
    /// and Material Design 3 two-layer (key + ambient) elevation shadow.
    /// </summary>
    public class MdSurface : MdContentView
    {
        public static readonly StyledProperty<Color> FillColorProperty =
            AvaloniaProperty.Register<MdSurface, Color>(nameof(FillColor), Colors.Transparent);

        public static readonly StyledProperty<Color?> SurfaceTintColorProperty =
            AvaloniaProperty.Register<MdSurface, Color?>(nameof(SurfaceTintColor));

        public static readonly StyledProperty<Color> BorderStrokeColorProperty =
            AvaloniaProperty.Register<MdSurface, Color>(nameof(BorderStrokeColor), Colors.Transparent);

        public static readonly StyledProperty<double> BorderStrokeThicknessProperty =
            AvaloniaProperty.Register<MdSurface, double>(nameof(BorderStrokeThickness));

        public static readonly StyledProperty<double> RadiusProperty =
            AvaloniaProperty.Register<MdSurface, double>(nameof(Radius));

        public static readonly StyledProperty<double> ElevationProperty =
            AvaloniaProperty.Register<MdSurface, double>(nameof(Elevation));

        private Color _fill = Colors.Transparent;
        private Color? _surfaceTint;
        private Color _stroke = Colors.Transparent;
        private double _strokeThickness;
        private double _elevation;

        static MdSurface()
        {
            FillColorProperty.Changed.AddClassHandler<MdSurface>((s, e) =>
            {
                s._fill = e.GetNewValue<Color>();
                s.UpdateFillBrush();
            });
            SurfaceTintColorProperty.Changed.AddClassHandler<MdSurface>((s, e) =>
            {
                s._surfaceTint = e.GetNewValue<Color?>();
                s.UpdateFillBrush();
            });
            BorderStrokeColorProperty.Changed.AddClassHandler<MdSurface>((s, e) =>
            {
                s._stroke = e.GetNewValue<Color>();
                s.BorderBrush = MdTheme.BrushOf(s._stroke);
            });
            BorderStrokeThicknessProperty.Changed.AddClassHandler<MdSurface>((s, e) =>
            {
                s._strokeThickness = e.GetNewValue<double>();
                s.BorderThickness = new Thickness(s._strokeThickness);
            });
            RadiusProperty.Changed.AddClassHandler<MdSurface>((s, e) =>
            {
                s.CornerRadius = new CornerRadius(Math.Max(0, e.GetNewValue<double>()));
            });
            ElevationProperty.Changed.AddClassHandler<MdSurface>((s, e) =>
            {
                s._elevation = Math.Max(0, e.GetNewValue<double>());
                s.BoxShadow = MdElevationTokens.ToBoxShadows(s._elevation);
                if (s._surfaceTint is not null)
                    s.UpdateFillBrush();
            });
        }

        public MdSurface()
        {
            BorderThickness = new Thickness(0);
        }

        public Color FillColor
        {
            get => _fill;
            set => SetValue(FillColorProperty, value);
        }

        /// <summary>
        /// Optional Material 3 surface-tint color blended over <see cref="FillColor"/> according to <see cref="Elevation"/>.
        /// </summary>
        public Color? SurfaceTintColor
        {
            get => _surfaceTint;
            set => SetValue(SurfaceTintColorProperty, value);
        }

        public Color BorderStrokeColor
        {
            get => _stroke;
            set => SetValue(BorderStrokeColorProperty, value);
        }

        public double BorderStrokeThickness
        {
            get => _strokeThickness;
            set => SetValue(BorderStrokeThicknessProperty, value);
        }

        public double Radius
        {
            get => CornerRadius.TopLeft;
            set => SetValue(RadiusProperty, value);
        }

        public double Elevation
        {
            get => _elevation;
            set => SetValue(ElevationProperty, value);
        }

        public void SetCornerRadii(double topLeft, double topRight, double bottomLeft, double bottomRight)
        {
            CornerRadius = new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
        }

        private void UpdateFillBrush()
        {
            var effective = _surfaceTint is { } tint && _elevation > 0
                ? MdElevationTokens.ApplySurfaceTint(_fill, tint, _elevation)
                : _fill;
            Background = MdTheme.BrushOf(effective);
        }
    }
}

namespace Avalonia.Controls
{
    public static class MdGridExtensions
    {
        public static void Add(
            this Grid grid,
            Control child,
            int column = 0,
            int row = 0,
            int columnSpan = 1,
            int rowSpan = 1)
        {
            if (column != 0)
                Grid.SetColumn(child, column);
            if (row != 0)
                Grid.SetRow(child, row);
            if (columnSpan > 1)
                Grid.SetColumnSpan(child, columnSpan);
            if (rowSpan > 1)
                Grid.SetRowSpan(child, rowSpan);
            grid.Children.Add(child);
        }
    }
}
