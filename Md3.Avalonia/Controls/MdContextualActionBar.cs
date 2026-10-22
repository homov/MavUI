using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Material Design 3 Contextual Action / Selection Bar that appears when one or more items
/// are selected in a list, grid, or data table, providing bulk actions and a clear-selection button.
/// </summary>
public class MdContextualActionBar : MdContentView, IEnumerable<Control>
{
    public static readonly StyledProperty<int> SelectedCountProperty =
        AvaloniaProperty.Register<MdContextualActionBar, int>(
            nameof(SelectedCount),
            0,
            defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> AutoHideWhenEmptyProperty =
        AvaloniaProperty.Register<MdContextualActionBar, bool>(nameof(AutoHideWhenEmpty), true);

    public static readonly StyledProperty<string> TitleFormatProperty =
        AvaloniaProperty.Register<MdContextualActionBar, string>(nameof(TitleFormat), string.Empty);

    public static readonly StyledProperty<ICommand?> ClearCommandProperty =
        AvaloniaProperty.Register<MdContextualActionBar, ICommand?>(nameof(ClearCommand));

    private readonly MdSurface _bar;
    private readonly MdIconButton _clearBtn;
    private readonly TextBlock _countLabel;
    private readonly StackPanel _actionsRow;
    private Func<int, string>? _titleFormatter;

    public event EventHandler? SelectionCleared;

    static MdContextualActionBar()
    {
        SelectedCountProperty.Changed.AddClassHandler<MdContextualActionBar>((b, _) => b.SyncState());
        AutoHideWhenEmptyProperty.Changed.AddClassHandler<MdContextualActionBar>((b, _) => b.SyncState());
        TitleFormatProperty.Changed.AddClassHandler<MdContextualActionBar>((b, _) => b.SyncState());
    }

    public MdContextualActionBar() : this(0, true)
    {
    }

    public MdContextualActionBar(int selectedCount = 0, bool autoHideWhenEmpty = true)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;

        _clearBtn = new MdIconButton(MdIcons.Close)
        {
            VerticalAlignment = VerticalAlignment.Center,
        };
        MdTooltip.SetText(_clearBtn, MdLocalization.ClearSelection);
        _clearBtn.Clicked += (_, _) =>
        {
            SelectedCount = 0;
            SelectionCleared?.Invoke(this, EventArgs.Empty);
            if (ClearCommand is { } cmd && cmd.CanExecute(null))
                cmd.Execute(null);
        };

        _countLabel = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 12, 0),
            FontWeight = FontWeight.SemiBold,
        };

        _actionsRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            MinHeight = 48,
        };
        row.Add(_clearBtn, 0, 0);
        row.Add(_countLabel, 1, 0);
        row.Add(_actionsRow, 2, 0);

        _bar = new MdSurface
        {
            Radius = 28,
            Elevation = 2,
            Padding = new Thickness(8, 4, 12, 4),
            Content = row,
        };

        Content = _bar;
        AutoHideWhenEmpty = autoHideWhenEmpty;
        SelectedCount = selectedCount;

        SyncState();
        MdLocalization.Observe(this, self =>
        {
            MdTooltip.SetText(self._clearBtn, MdLocalization.ClearSelection);
            self.SyncState();
        });
        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public int SelectedCount
    {
        get => GetValue(SelectedCountProperty);
        set => SetValue(SelectedCountProperty, Math.Max(0, value));
    }

    public bool AutoHideWhenEmpty
    {
        get => GetValue(AutoHideWhenEmptyProperty);
        set => SetValue(AutoHideWhenEmptyProperty, value);
    }

    public string TitleFormat
    {
        get => GetValue(TitleFormatProperty);
        set => SetValue(TitleFormatProperty, value);
    }

    public ICommand? ClearCommand
    {
        get => GetValue(ClearCommandProperty);
        set => SetValue(ClearCommandProperty, value);
    }

    public Func<int, string>? TitleFormatter
    {
        get => _titleFormatter;
        set
        {
            _titleFormatter = value;
            SyncState();
        }
    }

    [Content]
    public global::Avalonia.Controls.Controls Actions => _actionsRow.Children;

    public void Add(Control control) => _actionsRow.Children.Add(control);

    public IEnumerator<Control> GetEnumerator() => _actionsRow.Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public MdContextualActionBar AddAction(string icon, string? tooltip = null, Action? onClicked = null)
    {
        var btn = new MdIconButton(icon);
        if (!string.IsNullOrEmpty(tooltip))
            MdTooltip.SetText(btn, tooltip);
        if (onClicked is not null)
            btn.Clicked += (_, _) => onClicked();
        _actionsRow.Children.Add(btn);
        return this;
    }

    public MdContextualActionBar AddButton(string text, string? icon = null, Action? onClicked = null)
    {
        var btn = new MdButton(text, MdButtonVariant.Tonal, icon, MdButtonSize.Small);
        if (onClicked is not null)
            btn.Clicked += (_, _) => onClicked();
        _actionsRow.Children.Add(btn);
        return this;
    }

    private void SyncState()
    {
        var count = SelectedCount;
        var fmt = TitleFormat;
        if (string.IsNullOrEmpty(fmt))
            fmt = MdLocalization.SelectedCountFormat;

        string text;
        if (_titleFormatter is not null)
            text = _titleFormatter(count);
        else if (!string.IsNullOrEmpty(fmt) && fmt.Contains("{0}", StringComparison.Ordinal))
            text = string.Format(fmt, count);
        else
            text = MdLocalization.Format(
                MdLocalization.Keys.SelectedCountFormat, "{0} selected", count);
        _countLabel.Text = text;
        var shouldShow = !AutoHideWhenEmpty || count > 0;
        if (IsVisible != shouldShow)
        {
            IsVisible = shouldShow;
            if (shouldShow)
                MdMotion.FadeIn(_bar, MdMotion.Short3);
        }
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _bar.FillColor = c.SecondaryContainer;
        _countLabel.Apply(MdTypeRole.TitleMedium, t.Typography);
        _countLabel.Foreground = MdTheme.BrushOf(c.OnSecondaryContainer);
    }
}
