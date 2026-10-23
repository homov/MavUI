using System.Collections;
using System.Windows.Input;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Threading;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Material Design 3 Search View overlay with top query header, back navigation,
/// clear action, live suggestion filtering, and recent search history.
/// </summary>
public class MavSearchView : Control
{
    public static readonly StyledProperty<string> QueryProperty =
        AvaloniaProperty.Register<MavSearchView, string>(nameof(Query), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<MavSearchView, string>(nameof(Placeholder), MavLocalization.Search);

    public static readonly StyledProperty<bool> IsFullScreenProperty =
        AvaloniaProperty.Register<MavSearchView, bool>(nameof(IsFullScreen));

    public static readonly StyledProperty<System.Collections.IEnumerable?> SuggestionsSourceProperty =
        AvaloniaProperty.Register<MavSearchView, System.Collections.IEnumerable?>(nameof(SuggestionsSource));

    public static readonly StyledProperty<IDataTemplate?> ResultItemTemplateProperty =
        AvaloniaProperty.Register<MavSearchView, IDataTemplate?>(nameof(ResultItemTemplate));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MavSearchView, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> SearchCommandProperty =
        AvaloniaProperty.Register<MavSearchView, ICommand?>(nameof(SearchCommand));

    private readonly AvaloniaList<MavSearchSuggestion> _suggestionItems = new();
    private readonly List<(string Text, string? Subtitle, string Icon)> _suggestions = [];
    private readonly List<string> _recentQueries = [];
    private readonly MavCollectionObserver _observer;
    private IDisposable? _handle;
    private bool _syncingIsOpen;
    private IInputPane? _inputPane;
    private EventHandler<InputPaneStateEventArgs>? _inputPaneHandler;
    private Action? _refreshCardSize;

    static MavSearchView()
    {
        SuggestionsSourceProperty.Changed.AddClassHandler<MavSearchView>((v, e) =>
        {
            var src = e.GetNewValue<System.Collections.IEnumerable?>();
            v._observer.Attach(src);
            v.ReloadFromSource(src);
        });
        IsOpenProperty.Changed.AddClassHandler<MavSearchView>((v, e) =>
        {
            if (v._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                v.Show(v.Parent as Visual ?? v);
            else
                v.Close();
        });
    }

    /// <summary>Creates an empty search view.</summary>
    public MavSearchView()
    {
        Placeholder = MavLocalization.Search;
        _observer = new MavCollectionObserver(this, () => ReloadFromSource(SuggestionsSource));
        _suggestionItems.CollectionChanged += (_, _) =>
        {
            foreach (var item in _suggestionItems)
            {
                MavVisualTreeHelpers.AttachLogicalChild(this, item);
                if (item.IsRecent)
                    AddRecent(item.Text);
                else if (!_suggestions.Any(s => string.Equals(s.Text, item.Text, StringComparison.Ordinal)))
                    _suggestions.Add((item.Text, item.Subtitle, item.Icon));
            }
        };
    }

    /// <summary>
    /// Measures the search view during the layout pass.
    /// </summary>
    protected override Size MeasureOverride(Size availableSize) => default;

    [Content]
    public AvaloniaList<MavSearchSuggestion> Suggestions => _suggestionItems;

    public string Query
    {
        get => GetValue(QueryProperty);
        set => SetValue(QueryProperty, value);
    }

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public bool IsFullScreen
    {
        get => GetValue(IsFullScreenProperty);
        set => SetValue(IsFullScreenProperty, value);
    }

    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public IDataTemplate? ResultItemTemplate
    {
        get => GetValue(ResultItemTemplateProperty);
        set => SetValue(ResultItemTemplateProperty, value);
    }

    public ICommand? SearchCommand
    {
        get => GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public Func<string, IEnumerable<Control>>? CustomResultsBuilder { get; set; }
    public IReadOnlyList<string> RecentSearches => _recentQueries;

    public System.Collections.IEnumerable? SuggestionsSource
    {
        get => GetValue(SuggestionsSourceProperty);
        set => SetValue(SuggestionsSourceProperty, value);
    }

    public event EventHandler<string>? QueryChanged;
    public event EventHandler<string>? QuerySubmitted;
    public event EventHandler<string>? ResultSelected;
    public event EventHandler? Closed;

    /// <summary>Adds a suggestion and returns the view for chaining.</summary>
    public MavSearchView AddSuggestion(string text, string? subtitle = null, string icon = MavIcons.Search)
    {
        _suggestions.Add((text, subtitle, icon));
        return this;
    }

    /// <summary>Adds a recent query and returns the view for chaining.</summary>
    public MavSearchView AddRecent(string query)
    {
        if (!string.IsNullOrWhiteSpace(query) && !_recentQueries.Contains(query, StringComparer.OrdinalIgnoreCase))
            _recentQueries.Insert(0, query);
        return this;
    }

    /// <summary>
    /// Removes all recent search queries.
    /// </summary>
    public void ClearRecentSearches() => _recentQueries.Clear();

    private void ReloadFromSource(System.Collections.IEnumerable? source)
    {
        _suggestions.Clear();
        if (source is null) return;
        foreach (var item in source)
        {
            if (item is not null)
                _suggestions.Add((item.ToString() ?? string.Empty, null, MavIcons.Search));
        }
    }

    /// <summary>Opens the search view over <paramref name="anchor"/>.</summary>
    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MavTheme.Current;
        var c = t.Color;

        var backBtn = new MavIconButton(MavIcons.ArrowBack);
        backBtn.Clicked += (_, _) => Close();

        var entry = new MavNativeEntry
        {
            Text = Query,
            Placeholder = Placeholder,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(4, 0),
            FontFamily = t.Typography.PlainFont,
            FontSize = 16,
            TextColor = c.OnSurface,
            PlaceholderColor = c.OnSurfaceVariant,
        };
        TextInputOptions.SetContentType(entry, TextInputContentType.Search);
        TextInputOptions.SetReturnKeyType(entry, TextInputReturnKeyType.Search);

        var clearBtn = new MavIconButton(MavIcons.Close)
        {
            IsVisible = !string.IsNullOrEmpty(Query),
        };
        clearBtn.Clicked += (_, _) =>
        {
            entry.Text = string.Empty;
            entry.Focus();
        };

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            MinHeight = 64,
            Margin = new Thickness(8, 4, 8, 4),
        };
        headerRow.Add(backBtn, 0, 0);
        headerRow.Add(entry, 1, 0);
        headerRow.Add(clearBtn, 2, 0);

        var resultsStack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Spacing = 0,
            Margin = new Thickness(0, 4, 0, 12),
        };

        void RebuildResults(string q)
        {
            resultsStack.Children.Clear();
            var trimmed = q.Trim();

            if (CustomResultsBuilder is not null)
            {
                foreach (var ctrl in CustomResultsBuilder(trimmed))
                    resultsStack.Children.Add(ctrl);
                return;
            }

            if (string.IsNullOrEmpty(trimmed) && _recentQueries.Count > 0)
            {
                resultsStack.Children.Add(new MavListSubheader(MavLocalization.Get("SearchView.RecentSearches", "Recent searches")));
                foreach (var rec in _recentQueries.Take(5))
                {
                    var captured = rec;
                    var item = new MavListItem(rec, leadingIcon: MavIcons.History, trailingIcon: MavIcons.NorthWest);
                    item.Clicked += (_, _) =>
                    {
                        Query = captured;
                        AddRecent(captured);
                        ResultSelected?.Invoke(this, captured);
                        Close();
                    };
                    resultsStack.Children.Add(item);
                }
            }

            var filtered = string.IsNullOrEmpty(trimmed)
                ? _suggestions
                : _suggestions.Where(s =>
                    s.Text.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ||
                    (s.Subtitle?.Contains(trimmed, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            if (filtered.Count > 0)
            {
                resultsStack.Children.Add(new MavListSubheader(string.IsNullOrEmpty(trimmed)
                    ? MavLocalization.Get("SearchView.Suggestions", "Suggestions")
                    : MavLocalization.Get("SearchView.Results", "Results")));
                foreach (var (text, sub, icon) in filtered)
                {
                    var captured = text;
                    if (MavVisualTreeHelpers.BuildTemplateControl(captured, ResultItemTemplate) is { } templated)
                    {
                        templated.PointerReleased += (_, _) =>
                        {
                            Query = captured;
                            AddRecent(captured);
                            ResultSelected?.Invoke(this, captured);
                            Close();
                        };
                        resultsStack.Children.Add(templated);
                    }
                    else
                    {
                        var item = new MavListItem(text, sub, leadingIcon: icon);
                        item.Clicked += (_, _) =>
                        {
                            Query = captured;
                            AddRecent(captured);
                            ResultSelected?.Invoke(this, captured);
                            Close();
                        };
                        resultsStack.Children.Add(item);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(trimmed))
            {
                var emptyLbl = new MavLabel(MavLocalization.Format("SearchView.NoResults", "No results for \"{0}\"", trimmed), MavTypeRole.BodyMedium)
                {
                    Secondary = true,
                    Margin = new Thickness(20, 16),
                };
                resultsStack.Children.Add(emptyLbl);
            }
        }

        entry.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                Query = entry.Text ?? string.Empty;
                clearBtn.IsVisible = !string.IsNullOrEmpty(Query);
                RebuildResults(Query);
                QueryChanged?.Invoke(this, Query);
            }
        };

        entry.Completed += (_, _) =>
        {
            var q = entry.Text?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(q))
                AddRecent(q);
            QuerySubmitted?.Invoke(this, q);
            if (SearchCommand is { } sCmd && sCmd.CanExecute(q))
                sCmd.Execute(q);
            Close();
        };

        var rootGrid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,Auto,*"),
        };
        rootGrid.Add(headerRow, 0, 0);
        rootGrid.Add(new MavDivider(), 0, 1);
        rootGrid.Add(new MavScrollViewer { Content = resultsStack }, 0, 2);

        var effectiveAnchor = anchor ?? (this.Parent as Visual) ?? this;
        var topLevel = MavOverlay.ResolveTopLevel(effectiveAnchor);

        var card = new MavSurface
        {
            Radius = IsFullScreen ? 0 : 28,
            Elevation = 3,
            FillColor = c.SurfaceContainerHigh,
            HorizontalAlignment = IsFullScreen ? HorizontalAlignment.Stretch : HorizontalAlignment.Center,
            VerticalAlignment = IsFullScreen ? VerticalAlignment.Stretch : VerticalAlignment.Top,
            Child = IsFullScreen ? new MavSafeArea(rootGrid, MavSafeAreaEdges.All) : rootGrid,
        };

        // The card caps depend on the overlay size, the safe area and the soft keyboard,
        // all of which can change while the search view is open (resize, rotation, IME).
        void RefreshCardSize()
        {
            var current = MavOverlay.GetOverlaySize(effectiveAnchor);
            var insets = MavOverlay.SafeAreaOf(effectiveAnchor);
            var keyboard = topLevel?.InputPane is { State: InputPaneState.Open } open
                ? Math.Max(0, open.OccludedRect.Height)
                : 0;
            var width = Math.Max(0, current.Width - insets.Left - insets.Right - 32);
            var height = Math.Max(120, Math.Min(460, current.Height - insets.Top - insets.Bottom - keyboard - 48));

            if (IsFullScreen)
            {
                card.Margin = new Thickness(0);
                return;
            }

            card.MinWidth = Math.Min(320, width);
            card.MaxWidth = Math.Min(600, width);
            card.MaxHeight = height;
            card.Margin = new Thickness(
                16 + insets.Left,
                24 + insets.Top,
                16 + insets.Right,
                24 + insets.Bottom);
        }

        RefreshCardSize();

        if (!IsFullScreen)
        {
            _refreshCardSize = RefreshCardSize;
            if (topLevel?.InputPane is { } inputPane)
            {
                _inputPane = inputPane;
                _inputPaneHandler = (_, _) =>
                    Dispatcher.UIThread.Post(() => _refreshCardSize?.Invoke(), DispatcherPriority.Loaded);
                inputPane.StateChanged += _inputPaneHandler;
            }
        }

        RebuildResults(Query);

        IDisposable? handle = null;
        handle = MavOverlay.Show(
            effectiveAnchor,
            card,
            scrimColor: c.Scrim.WithAlpha(0.32f),
            onScrimTap: Close,
            onViewportChanged: RefreshCardSize,
            onClosed: () => OnOverlayClosed(handle));
        _handle = handle;

        _syncingIsOpen = true;
        SetValue(IsOpenProperty, true);
        _syncingIsOpen = false;

        MavMotion.ScaleIn(card, 0.94, MavMotion.Medium1);
        Dispatcher.UIThread.Post(() => entry.Focus(), DispatcherPriority.Input);
    }

    /// <summary>Closes the search view.</summary>
    public void Close()
    {
        var handle = _handle;
        if (handle is null)
            return;

        _handle = null;
        handle.Dispose();
        FinishClose();
    }

    private void OnOverlayClosed(IDisposable? handle)
    {
        if (handle is null || !ReferenceEquals(_handle, handle))
            return;

        _handle = null;
        FinishClose();
    }

    private void FinishClose()
    {
        DetachInputPane();
        _syncingIsOpen = true;
        SetValue(IsOpenProperty, false);
        _syncingIsOpen = false;
        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void DetachInputPane()
    {
        _refreshCardSize = null;
        if (_inputPane is not null && _inputPaneHandler is not null)
            _inputPane.StateChanged -= _inputPaneHandler;
        _inputPane = null;
        _inputPaneHandler = null;
    }
}
