using System.Collections;
using System.Collections.Specialized;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 1dp horizontal or vertical divider with optional leading/trailing insets.
/// </summary>
public class MavDivider : Border
{
    public static readonly StyledProperty<bool> IsVerticalProperty =
        AvaloniaProperty.Register<MavDivider, bool>(nameof(IsVertical));

    public static readonly StyledProperty<double> InsetStartProperty =
        AvaloniaProperty.Register<MavDivider, double>(nameof(InsetStart));

    public static readonly StyledProperty<double> InsetEndProperty =
        AvaloniaProperty.Register<MavDivider, double>(nameof(InsetEnd));

    static MavDivider()
    {
        IsVerticalProperty.Changed.AddClassHandler<MavDivider>((d, _) => d.SyncMetrics());
        InsetStartProperty.Changed.AddClassHandler<MavDivider>((d, _) => d.SyncMetrics());
        InsetEndProperty.Changed.AddClassHandler<MavDivider>((d, _) => d.SyncMetrics());
    }

    /// <summary>Creates a horizontal divider without insets.</summary>
    public MavDivider() : this(0, 0, false)
    {
    }

    /// <summary>Creates a divider with the given insets and orientation.</summary>
    public MavDivider(double insetStart = 0, double insetEnd = 0, bool vertical = false)
    {
        InsetStart = insetStart;
        InsetEnd = insetEnd;
        IsVertical = vertical;
        SyncMetrics();

        MavTheme.Observe(this, (self, theme) =>
        {
            self.Background = MavTheme.BrushOf(theme.Color.OutlineVariant);
        });
    }

    public bool IsVertical
    {
        get => GetValue(IsVerticalProperty);
        set => SetValue(IsVerticalProperty, value);
    }

    public double InsetStart
    {
        get => GetValue(InsetStartProperty);
        set => SetValue(InsetStartProperty, value);
    }

    public double InsetEnd
    {
        get => GetValue(InsetEndProperty);
        set => SetValue(InsetEndProperty, value);
    }

    private void SyncMetrics()
    {
        if (IsVertical)
        {
            Width = 1;
            Height = double.NaN;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Stretch;
            Margin = new Thickness(0, InsetStart, 0, InsetEnd);
        }
        else
        {
            Height = 1;
            Width = double.NaN;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Center;
            Margin = new Thickness(InsetStart, 0, InsetEnd, 0);
        }
    }
}
