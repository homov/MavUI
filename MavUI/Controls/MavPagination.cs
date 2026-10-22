using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Pagination control with smart page windowing, ellipsis indicators,
/// first/previous/next/last navigation buttons, and TwoWay CurrentPage binding.
/// </summary>
public class MavPagination : MavContentView
{
    public static readonly StyledProperty<int> TotalPagesProperty =
        AvaloniaProperty.Register<MavPagination, int>(nameof(TotalPages), 1);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<MavPagination, int>(nameof(CurrentPage), 1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<int> MaxVisiblePagesProperty =
        AvaloniaProperty.Register<MavPagination, int>(nameof(MaxVisiblePages), 5);

    public static readonly StyledProperty<bool> ShowFirstLastButtonsProperty =
        AvaloniaProperty.Register<MavPagination, bool>(nameof(ShowFirstLastButtons), true);

    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<MavPagination, ICommand?>(nameof(Command));

    private readonly StackPanel _row;

    public event EventHandler<int>? PageChanged;

    static MavPagination()
    {
        TotalPagesProperty.Changed.AddClassHandler<MavPagination>((p, _) => p.Rebuild());
        CurrentPageProperty.Changed.AddClassHandler<MavPagination>((p, e) =>
        {
            p.Rebuild();
            var page = e.GetNewValue<int>();
            p.PageChanged?.Invoke(p, page);
            if (p.Command?.CanExecute(page) == true)
                p.Command.Execute(page);
        });
        MaxVisiblePagesProperty.Changed.AddClassHandler<MavPagination>((p, _) => p.Rebuild());
        ShowFirstLastButtonsProperty.Changed.AddClassHandler<MavPagination>((p, _) => p.Rebuild());
    }

    /// <summary>Creates a pagination control with ten pages, starting at page 1.</summary>
    public MavPagination() : this(10, 1)
    {
    }

    /// <summary>Creates a pagination control for <paramref name="totalPages"/> pages, starting at <paramref name="currentPage"/>.</summary>
    public MavPagination(int totalPages = 10, int currentPage = 1)
    {
        _row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        Content = _row;

        TotalPages = Math.Max(1, totalPages);
        CurrentPage = Math.Clamp(currentPage, 1, TotalPages);

        MavTheme.Observe(this, (self, _) => self.Rebuild());
    }

    public int TotalPages
    {
        get => GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, Math.Max(1, value));
    }

    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, Math.Clamp(value, 1, Math.Max(1, TotalPages)));
    }

    public int MaxVisiblePages
    {
        get => GetValue(MaxVisiblePagesProperty);
        set => SetValue(MaxVisiblePagesProperty, Math.Max(3, value));
    }

    public bool ShowFirstLastButtons
    {
        get => GetValue(ShowFirstLastButtonsProperty);
        set => SetValue(ShowFirstLastButtonsProperty, value);
    }

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    private void Rebuild()
    {
        _row.Children.Clear();
        var t = MavTheme.Current;
        var total = Math.Max(1, TotalPages);
        var current = Math.Clamp(CurrentPage, 1, total);

        if (ShowFirstLastButtons)
            _row.Children.Add(BuildNavButton(MavIcons.FirstPage, current > 1, () => CurrentPage = 1, t));

        _row.Children.Add(BuildNavButton(MavIcons.ChevronLeft, current > 1, () => CurrentPage = current - 1, t));

        foreach (var token in ComputePageTokens(total, current, Math.Max(3, MaxVisiblePages)))
        {
            if (token < 0)
            {
                var dots = new TextBlock
                {
                    Text = "…",
                    Width = 32,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = MavTheme.BrushOf(t.Color.OnSurfaceVariant),
                };
                dots.Apply(MavTypeRole.LabelLarge, t.Typography);
                _row.Children.Add(dots);
            }
            else
            {
                _row.Children.Add(BuildPagePill(token, token == current, t));
            }
        }

        _row.Children.Add(BuildNavButton(MavIcons.ChevronRight, current < total, () => CurrentPage = current + 1, t));

        if (ShowFirstLastButtons)
            _row.Children.Add(BuildNavButton(MavIcons.LastPage, current < total, () => CurrentPage = total, t));
    }

    private static List<int> ComputePageTokens(int total, int current, int maxVisible)
    {
        var result = new List<int>();
        if (total <= maxVisible + 2)
        {
            for (var i = 1; i <= total; i++) result.Add(i);
            return result;
        }

        var half = maxVisible / 2;
        var start = Math.Max(2, current - half);
        var end = Math.Min(total - 1, start + maxVisible - 1);
        start = Math.Max(2, end - maxVisible + 1);

        result.Add(1);
        if (start > 2) result.Add(-1);
        for (var i = start; i <= end; i++) result.Add(i);
        if (end < total - 1) result.Add(-1);
        result.Add(total);
        return result;
    }

    private Control BuildNavButton(string glyph, bool enabled, Action onClick, MavTheme t)
    {
        var c = t.Color;
        var icon = new MavIcon(glyph, 18)
        {
            UseThemeDefaultColor = false,
            Color = enabled ? c.OnSurface : c.OnSurface.WithAlpha(0.35f),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        var surf = new MavInkSurface
        {
            Width = 36,
            Height = 36,
            CornerRadius = 18,
            FillColor = Colors.Transparent,
            RippleColor = c.OnSurface,
            IsEnabled = enabled,
            Content = icon,
        };
        surf.Clicked += (_, _) =>
        {
            if (enabled) onClick();
        };
        return surf;
    }

    private Control BuildPagePill(int page, bool isSelected, MavTheme t)
    {
        var c = t.Color;
        var lbl = new TextBlock
        {
            Text = page.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = MavTheme.BrushOf(isSelected ? c.OnPrimary : c.OnSurface),
        };
        lbl.Apply(MavTypeRole.LabelLarge, t.Typography);

        var surf = new MavInkSurface
        {
            MinWidth = 36,
            Height = 36,
            CornerRadius = isSelected ? 12 : 18,
            Padding = new Thickness(10, 0),
            FillColor = isSelected ? c.Primary : c.SurfaceContainerLow,
            StrokeThickness = isSelected ? 0 : 1,
            StrokeColor = c.OutlineVariant.WithAlpha(0.5f),
            RippleColor = isSelected ? c.OnPrimary : c.Primary,
            Content = lbl,
        };
        surf.Clicked += (_, _) => CurrentPage = page;
        return surf;
    }
}
