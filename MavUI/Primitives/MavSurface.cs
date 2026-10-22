using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Theming;

namespace Mav.Primitives
{
    /// <summary>
    /// Shared drawing helpers for Material Design 3 arc paths and text field chrome (Filled / Outlined with notch cutout).
    /// </summary>
    internal static class MavDrawingHelpers
    {
        /// <summary>
        /// Draws an arc of a circle, starting at the given angle and sweeping by the given angle.
        /// </summary>
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

        /// <summary>
        /// Draws the outline of a text field, with a notch for its floating label.
        /// </summary>
        public static void DrawFieldChrome(
            DrawingContext context,
            double w,
            double h,
            bool isFilled,
            bool isEnabled,
            bool hasError,
            bool isFocused,
            string? label,
            double floatProgress,
            MavFieldChromeCache? outlineCache = null)
        {
            if (w <= 0 || h <= 0) return;

            var c = MavTheme.Current.Color;
            var strokeColor = !isEnabled
                ? c.OnSurface.WithAlpha(0.12f)
                : hasError
                    ? c.Error
                    : isFocused
                        ? c.Primary
                        : c.Outline;
            var strokeW = isFocused ? 2.0 : 1.0;
            var pen = MavTheme.PenOf(strokeColor, strokeW);

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
                context.DrawRectangle(MavTheme.BrushOf(fill), null, rrect);
                context.DrawLine(
                    pen,
                    new Point(0, h - strokeW / 2.0),
                    new Point(w, h - strokeW / 2.0));
                return;
            }

            var inset = strokeW / 2.0;
            var right = w - inset;
            const double r = 4.0;

            var hasNotch = !string.IsNullOrEmpty(label) && floatProgress > 0.1;
            var notchWidth = hasNotch
                ? MavTextMetrics.MeasureWidth(label, 12, bold: true) * floatProgress + 8.0
                : 0.0;
            var notchStart = 12.0;
            var notchEnd = Math.Min(right - r - 4, notchStart + notchWidth);

            var geom = outlineCache is not null
                ? outlineCache.Get(w, h, strokeW, hasNotch ? notchEnd : 0)
                : CreateFieldOutline(w, h, strokeW, hasNotch ? notchEnd : 0);

            context.DrawGeometry(null, pen, geom);
        }

        /// <summary>
        /// Builds the outlined field path. <paramref name="notchEnd"/> is 0 when the
        /// floating label does not cut the top border.
        /// </summary>
        internal static Geometry CreateFieldOutline(double w, double h, double strokeW, double notchEnd)
        {
            var inset = strokeW / 2.0;
            var left = inset;
            var top = inset;
            var right = w - inset;
            var bottom = h - inset;
            const double r = 4.0;
            const double notchStart = 12.0;
            var hasNotch = notchEnd > notchStart + 2;

            var geom = new StreamGeometry();
            using var ctx = geom.Open();
            if (hasNotch)
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

            return geom;
        }
    }

    /// <summary>
    /// Per-control geometry cache for the outlined text-field chrome. The key covers
    /// size, stroke width (focus changes the inset) and the notch cutout end.
    /// </summary>
    internal sealed class MavFieldChromeCache
    {
        private FieldKey _key;
        private Geometry? _geometry;
        private bool _hasKey;

        /// <summary>
        /// Returns the field outline geometry, reusing the cached geometry when the size is unchanged.
        /// </summary>
        public Geometry Get(double width, double height, double strokeWidth, double notchEnd)
        {
            var key = new FieldKey(width, height, strokeWidth, notchEnd);
            if (_hasKey && key == _key && _geometry is not null)
                return _geometry;

            _key = key;
            _hasKey = true;
            _geometry = MavDrawingHelpers.CreateFieldOutline(width, height, strokeWidth, notchEnd);
            return _geometry;
        }

        private readonly record struct FieldKey(
            double Width,
            double Height,
            double StrokeWidth,
            double NotchEnd);
    }

    /// <summary>
    /// Marker interface implemented by interactive drag controls (sliders, gauges, dials, color pickers, carousels, swipe items)
    /// so parent gesture hosts such as <see cref="Controls.MavRefreshIndicator"/> do not steal pointer capture.
    /// </summary>
    public interface IMavDraggableControl
    {
    }

    /// <summary>
    /// Shared visual-tree and collection utilities used across MD3 controls.
    /// </summary>
    internal static class MavVisualTreeHelpers
    {
        /// <summary>
        /// Removes the control from its current parent so it can be attached elsewhere.
        /// </summary>
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

        /// <summary>
        /// Adds the child as a logical child of the parent.
        /// </summary>
        public static void AttachLogicalChild(StyledElement parent, StyledElement? child)
        {
            if (child is null || ReferenceEquals(child.Parent, parent)) return;
            if (child.Parent is null && child is ISetLogicalParent slp)
                slp.SetParent(parent);
        }

        /// <summary>
        /// Builds the control that displays an item, using the data template or the factory.
        /// </summary>
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
        /// <summary>
        /// Formats an item for display, using the given format or the item's string value.
        /// </summary>
        public static string FormatItemText(
            object? item,
            string? displayMemberPath,
            Func<object, string>? formatter = null)
        {
            if (item is null) return string.Empty;
            if (formatter is not null) return formatter(item);
            if (!string.IsNullOrWhiteSpace(displayMemberPath))
            {
                var prop = MavPropertyCache.Get(item.GetType(), displayMemberPath);
                if (prop is not null)
                    return prop.GetValue(item)?.ToString() ?? string.Empty;
            }
            return item.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Manages <see cref="INotifyCollectionChanged"/> subscriptions for <c>ItemsSource</c> properties.
    /// The owner's visual-tree lifetime bounds the subscription so a long-lived source collection
    /// cannot keep a detached control alive.
    /// </summary>
    internal sealed class MavCollectionObserver
    {
        private readonly Control _owner;
        private readonly Action _onChanged;
        private readonly Action<NotifyCollectionChangedEventArgs>? _onDetailedChanged;
        private readonly NotifyCollectionChangedEventHandler _handler;
        private INotifyCollectionChanged? _current;
        private IEnumerable? _source;
        private bool _inTree;
        private bool _needsRefresh;

        /// <summary>
        /// Creates an observer that tracks changes to a collection.
        /// </summary>
        public MavCollectionObserver(
            Control owner,
            Action onChanged,
            Action<NotifyCollectionChangedEventArgs>? onDetailedChanged = null)
        {
            _owner = owner;
            _onChanged = onChanged;
            _onDetailedChanged = onDetailedChanged;
            _handler = OnCollectionChanged;
            _inTree = owner.IsAttachedToVisualTree();

            _owner.AttachedToVisualTree += OnAttached;
            _owner.DetachedFromVisualTree += OnDetached;
        }

        /// <summary>
        /// Starts observing the collection, or stops when source is null.
        /// </summary>
        public void Attach(IEnumerable? source)
        {
            _source = source;
            if (_inTree)
                Subscribe();
            else
                Unsubscribe();
        }

        private void OnAttached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (_inTree) return;
            _inTree = true;
            Subscribe();

            if (!_needsRefresh) return;
            _needsRefresh = false;
            _onChanged();
        }

        private void OnDetached(object? sender, VisualTreeAttachmentEventArgs e)
        {
            if (!_inTree) return;
            _inTree = false;

            // The source may have changed while we were detached. Rebuild once when
            // the owner returns to the visual tree so no updates are lost.
            _needsRefresh = _current is not null;
            Unsubscribe();
        }

        private void Subscribe()
        {
            Unsubscribe();
            if (_source is not INotifyCollectionChanged incc) return;

            _current = incc;
            _current.CollectionChanged += _handler;
        }

        private void Unsubscribe()
        {
            if (_current is null) return;

            _current.CollectionChanged -= _handler;
            _current = null;
        }

        private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (_onDetailedChanged is not null)
                _onDetailedChanged(e);
            else
                _onChanged();
        }
    }
    /// <summary>
    /// Lightweight single-child container base class (Avalonia counterpart of MAUI ContentView)
    /// backed directly by <see cref="Border"/> with zero template overhead.
    /// </summary>
    public class MavContentView : Border
    {
        public static readonly StyledProperty<Control?> ContentProperty =
            ChildProperty.AddOwner<MavContentView>();

        public Control? Content
        {
            get => Child;
            set => Child = value;
        }

        public Color BackgroundColor
        {
            get => (Background as ISolidColorBrush)?.Color ?? Colors.Transparent;
            set => Background = value.A == 0 ? null : MavTheme.BrushOf(value);
        }
    }

    /// <summary>
    /// Rounded container with configurable corner radii, border stroke, tonal surface tint,
    /// and Material Design 3 two-layer (key + ambient) elevation shadow.
    /// </summary>
    public class MavSurface : MavContentView
    {
        public static readonly StyledProperty<Color> FillColorProperty =
            AvaloniaProperty.Register<MavSurface, Color>(nameof(FillColor), Colors.Transparent);

        public static readonly StyledProperty<Color?> SurfaceTintColorProperty =
            AvaloniaProperty.Register<MavSurface, Color?>(nameof(SurfaceTintColor));

        public static readonly StyledProperty<Color> BorderStrokeColorProperty =
            AvaloniaProperty.Register<MavSurface, Color>(nameof(BorderStrokeColor), Colors.Transparent);

        public static readonly StyledProperty<double> BorderStrokeThicknessProperty =
            AvaloniaProperty.Register<MavSurface, double>(nameof(BorderStrokeThickness));

        public static readonly StyledProperty<double> RadiusProperty =
            AvaloniaProperty.Register<MavSurface, double>(nameof(Radius));

        public static readonly StyledProperty<double> ElevationProperty =
            AvaloniaProperty.Register<MavSurface, double>(nameof(Elevation));

        private Color _fill = Colors.Transparent;
        private Color? _surfaceTint;
        private Color _stroke = Colors.Transparent;
        private double _strokeThickness;
        private double _elevation;

        static MavSurface()
        {
            FillColorProperty.Changed.AddClassHandler<MavSurface>((s, e) =>
            {
                s._fill = e.GetNewValue<Color>();
                s.UpdateFillBrush();
            });
            SurfaceTintColorProperty.Changed.AddClassHandler<MavSurface>((s, e) =>
            {
                s._surfaceTint = e.GetNewValue<Color?>();
                s.UpdateFillBrush();
            });
            BorderStrokeColorProperty.Changed.AddClassHandler<MavSurface>((s, e) =>
            {
                s._stroke = e.GetNewValue<Color>();
                s.BorderBrush = MavTheme.BrushOf(s._stroke);
            });
            BorderStrokeThicknessProperty.Changed.AddClassHandler<MavSurface>((s, e) =>
            {
                s._strokeThickness = e.GetNewValue<double>();
                s.BorderThickness = new Thickness(s._strokeThickness);
            });
            RadiusProperty.Changed.AddClassHandler<MavSurface>((s, e) =>
            {
                s.CornerRadius = new CornerRadius(Math.Max(0, e.GetNewValue<double>()));
            });
            ElevationProperty.Changed.AddClassHandler<MavSurface>((s, e) =>
            {
                s._elevation = Math.Max(0, e.GetNewValue<double>());
                s.BoxShadow = MavElevationTokens.ToBoxShadows(s._elevation);
                if (s._surfaceTint is not null)
                    s.UpdateFillBrush();
            });
        }

        /// <summary>
        /// Creates a surface with the default shape and colors.
        /// </summary>
        public MavSurface()
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

        /// <summary>
        /// Sets the corner radii of the surface.
        /// </summary>
        public void SetCornerRadii(double topLeft, double topRight, double bottomLeft, double bottomRight)
        {
            CornerRadius = new CornerRadius(topLeft, topRight, bottomRight, bottomLeft);
        }

        private void UpdateFillBrush()
        {
            var effective = _surfaceTint is { } tint && _elevation > 0
                ? MavElevationTokens.ApplySurfaceTint(_fill, tint, _elevation)
                : _fill;
            Background = MavTheme.BrushOf(effective);
        }
    }
}

namespace Avalonia.Controls
{
    /// <summary>Extension methods for building grid layouts with <see cref="MavSurface"/> and related panels.</summary>
    public static class MavGridExtensions
    {
        /// <summary>
        /// Adds a control to the grid at the given column and row.
        /// </summary>
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
