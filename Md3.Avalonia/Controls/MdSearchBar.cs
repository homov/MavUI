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
using Md3.Avalonia.Platform;
using Md3.Avalonia.Primitives;
using Md3.Avalonia.Theming;

namespace Md3.Avalonia.Controls;

/// <summary>
/// Declarative suggestion or recent-query item for <see cref="MdSearchView"/>.
/// </summary>
public sealed class MdSearchSuggestion : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdSearchSuggestion, string>(nameof(Text), string.Empty);

    public static readonly StyledProperty<string?> SubtitleProperty =
        AvaloniaProperty.Register<MdSearchSuggestion, string?>(nameof(Subtitle));

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<MdSearchSuggestion, string>(nameof(Icon), MdIcons.Search);

    public static readonly StyledProperty<bool> IsRecentProperty =
        AvaloniaProperty.Register<MdSearchSuggestion, bool>(nameof(IsRecent));

    public MdSearchSuggestion()
    {
    }

    public MdSearchSuggestion(string text, string? subtitle = null, string icon = MdIcons.Search, bool isRecent = false)
    {
        Text = text;
        Subtitle = subtitle;
        Icon = icon;
        IsRecent = isRecent;
    }

    [Content]
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public string? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public string Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public bool IsRecent
    {
        get => GetValue(IsRecentProperty);
        set => SetValue(IsRecentProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize) => default;
}

/// <summary>
/// Material Design 3 pill Search Bar (56dp height, SurfaceContainerHigh, leading search icon,
/// clear button, and optional trailing avatar or icon).
/// </summary>
public class MdSearchBar : MdContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MdSearchBar, string>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<MdSearchBar, string>(nameof(Placeholder), "Search");

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MdSearchBar, string?>(nameof(LeadingIcon), MdIcons.Search);

    public static readonly StyledProperty<string?> TrailingIconProperty =
        AvaloniaProperty.Register<MdSearchBar, string?>(nameof(TrailingIcon));

    public static readonly StyledProperty<Control?> TrailingViewProperty =
        AvaloniaProperty.Register<MdSearchBar, Control?>(nameof(TrailingView));

    public static readonly StyledProperty<ICommand?> SearchCommandProperty =
        AvaloniaProperty.Register<MdSearchBar, ICommand?>(nameof(SearchCommand));

    public static readonly StyledProperty<object?> SearchCommandParameterProperty =
        AvaloniaProperty.Register<MdSearchBar, object?>(nameof(SearchCommandParameter));

    public static readonly StyledProperty<ICommand?> LeadingIconCommandProperty =
        AvaloniaProperty.Register<MdSearchBar, ICommand?>(nameof(LeadingIconCommand));

    public static readonly StyledProperty<ICommand?> TrailingIconCommandProperty =
        AvaloniaProperty.Register<MdSearchBar, ICommand?>(nameof(TrailingIconCommand));

    private readonly MdSurface _pill;
    private readonly MdIcon _leadIcon;
    private readonly MdNativeEntry _entry;
    private readonly Border _clearHit;
    private readonly MdIcon _clearIcon;
    private readonly Border _trailHit;
    private readonly MdIcon _trailIcon;
    private Control? _trailingView;
    private bool _updating;

    public event EventHandler<string>? TextChanged;
    public event EventHandler<string>? SearchSubmitted;
    public event EventHandler? LeadingIconClicked;
    public event EventHandler? TrailingIconClicked;

    static MdSearchBar()
    {
        TextProperty.Changed.AddClassHandler<MdSearchBar>((s, e) => s.OnOuterTextChanged(e.GetNewValue<string>()));
        PlaceholderProperty.Changed.AddClassHandler<MdSearchBar>((s, e) => s._entry.Placeholder = e.GetNewValue<string>());
        LeadingIconProperty.Changed.AddClassHandler<MdSearchBar>((s, e) => s._leadIcon.Glyph = e.GetNewValue<string?>());
        TrailingIconProperty.Changed.AddClassHandler<MdSearchBar>((s, _) => s.SyncTrailing());
        TrailingViewProperty.Changed.AddClassHandler<MdSearchBar>((s, e) =>
        {
            s._trailingView = e.GetNewValue<Control?>();
            MdVisualTreeHelpers.DetachFromParent(s._trailingView);
            s.SyncTrailing();
        });
    }

    public MdSearchBar() : this(MdLocalization.Search, null)
    {
    }

    public MdSearchBar(string? placeholder = null, string? trailingIcon = null)
    {
        placeholder ??= MdLocalization.Search;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;

        _leadIcon = new MdIcon(MdIcons.Search, 24) { UseThemeDefaultColor = false };
        var leadHit = new Border
        {
            Width = 40,
            Height = 40,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            Child = _leadIcon,
        };
        leadHit.PointerReleased += (_, _) =>
        {
            LeadingIconClicked?.Invoke(this, EventArgs.Empty);
            if (LeadingIconCommand is { } lCmd && lCmd.CanExecute(null))
                lCmd.Execute(null);
        };

        _entry = new MdNativeEntry
        {
            Placeholder = placeholder,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Margin = new Thickness(8, 0),
        };
        _entry.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
                OnEntryTextChanged(_entry.Text ?? string.Empty);
        };
        TextInputOptions.SetContentType(_entry, TextInputContentType.Search);
        TextInputOptions.SetReturnKeyType(_entry, TextInputReturnKeyType.Search);
        _entry.Completed += (_, _) =>
        {
            SearchSubmitted?.Invoke(this, Text);
            var param = SearchCommandParameter ?? Text;
            if (SearchCommand is { } sCmd && sCmd.CanExecute(param))
                sCmd.Execute(param);
        };

        _clearIcon = new MdIcon(MdIcons.Close, 20) { UseThemeDefaultColor = false };
        _clearHit = new Border
        {
            Width = 36,
            Height = 36,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            IsVisible = false,
            Child = _clearIcon,
        };
        _clearHit.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            Text = string.Empty;
            _entry.Focus();
        };

        _trailIcon = new MdIcon { Size = 22, UseThemeDefaultColor = false };
        _trailHit = new Border
        {
            Width = 36,
            Height = 36,
            Background = Brushes.Transparent,
            VerticalAlignment = VerticalAlignment.Center,
            Cursor = new Cursor(StandardCursorType.Hand),
            IsVisible = false,
            Child = _trailIcon,
        };
        _trailHit.PointerReleased += (_, e) =>
        {
            e.Handled = true;
            TrailingIconClicked?.Invoke(this, EventArgs.Empty);
            if (TrailingIconCommand is { } tCmd && tCmd.CanExecute(null))
                tCmd.Execute(null);
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto"),
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        row.Add(leadHit, 0, 0);
        row.Add(_entry, 1, 0);
        row.Add(_clearHit, 2, 0);
        row.Add(_trailHit, 3, 0);

        _pill = new MdSurface
        {
            MinHeight = 56,
            Radius = 28,
            Padding = new Thickness(8, 0, 12, 0),
            Content = row,
        };
        _pill.PointerPressed += (_, _) =>
        {
            if (IsEnabled && !_entry.IsFocused)
                _entry.Focus();
        };
        _pill.ContextRequested += (_, e) =>
        {
            if (!e.Handled && IsEnabled)
            {
                if (!_entry.IsFocused)
                    _entry.Focus();
                _entry.RaiseEvent(e);
            }
        };

        Content = _pill;
        Placeholder = placeholder;
        TrailingIcon = trailingIcon;
        SyncTrailing();

        MdTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
    }

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value ?? string.Empty);
    }

    public string Query
    {
        get => Text;
        set => Text = value;
    }

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string? LeadingIcon
    {
        get => GetValue(LeadingIconProperty);
        set => SetValue(LeadingIconProperty, value);
    }

    public string? TrailingIcon
    {
        get => GetValue(TrailingIconProperty);
        set => SetValue(TrailingIconProperty, value);
    }

    public Control? TrailingView
    {
        get => GetValue(TrailingViewProperty);
        set => SetValue(TrailingViewProperty, value);
    }

    public ICommand? SearchCommand
    {
        get => GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    public object? SearchCommandParameter
    {
        get => GetValue(SearchCommandParameterProperty);
        set => SetValue(SearchCommandParameterProperty, value);
    }

    public ICommand? LeadingIconCommand
    {
        get => GetValue(LeadingIconCommandProperty);
        set => SetValue(LeadingIconCommandProperty, value);
    }

    public ICommand? TrailingIconCommand
    {
        get => GetValue(TrailingIconCommandProperty);
        set => SetValue(TrailingIconCommandProperty, value);
    }

    public MdNativeEntry InnerEntry => _entry;

    private void OnOuterTextChanged(string? val)
    {
        if (_updating) return;
        _updating = true;
        _entry.Text = val ?? string.Empty;
        _updating = false;
        _clearHit.IsVisible = !string.IsNullOrEmpty(val);
        TextChanged?.Invoke(this, _entry.Text ?? string.Empty);
    }

    private void OnEntryTextChanged(string val)
    {
        if (_updating) return;
        _updating = true;
        SetValue(TextProperty, val);
        _updating = false;
        _clearHit.IsVisible = !string.IsNullOrEmpty(val);
        TextChanged?.Invoke(this, val);
    }

    private void SyncTrailing()
    {
        if (_trailingView is not null)
        {
            if (!ReferenceEquals(_trailHit.Child, _trailingView))
            {
                _trailHit.Child = null;
                MdVisualTreeHelpers.DetachFromParent(_trailingView);
                _trailHit.Child = _trailingView;
            }
            _trailHit.IsVisible = true;
        }
        else if (!string.IsNullOrEmpty(TrailingIcon))
        {
            _trailIcon.Glyph = TrailingIcon;
            if (!ReferenceEquals(_trailHit.Child, _trailIcon))
            {
                _trailHit.Child = null;
                MdVisualTreeHelpers.DetachFromParent(_trailIcon);
                _trailHit.Child = _trailIcon;
            }
            _trailHit.IsVisible = true;
        }
        else
        {
            _trailHit.IsVisible = false;
        }
    }

    private void ApplyTheme(MdTheme t)
    {
        var c = t.Color;
        _pill.FillColor = c.SurfaceContainerHigh;
        _entry.FontFamily = t.Typography.PlainFont;
        _entry.FontSize = 16;
        _entry.TextColor = c.OnSurface;
        _entry.PlaceholderColor = c.OnSurfaceVariant;
        _leadIcon.Color = c.OnSurface;
        _clearIcon.Color = c.OnSurfaceVariant;
        _trailIcon.Color = c.OnSurfaceVariant;
    }

    /// <summary>
    /// Attaches an <see cref="MdSearchView"/> so tapping or focusing this search bar opens the full Material 3 Search View.
    /// </summary>
    public MdSearchView AttachSearchView(IEnumerable<string>? suggestions = null, bool fullScreen = false)
    {
        var view = new MdSearchView
        {
            Placeholder = Placeholder,
            IsFullScreen = fullScreen,
        };
        if (suggestions is not null)
        {
            foreach (var s in suggestions)
                view.AddSuggestion(s);
        }

        _entry.GotFocus += (_, _) =>
        {
            _entry.Unfocus();
            view.Query = Text;
            view.Show(this);
        };

        view.ResultSelected += (_, selected) =>
        {
            Text = selected;
            SearchSubmitted?.Invoke(this, selected);
        };
        view.QuerySubmitted += (_, q) =>
        {
            Text = q;
            SearchSubmitted?.Invoke(this, q);
        };

        return view;
    }
}

/// <summary>
/// Material Design 3 Search View overlay with top query header, back navigation,
/// clear action, live suggestion filtering, and recent search history.
/// </summary>
public class MdSearchView : Control
{
    public static readonly StyledProperty<string> QueryProperty =
        AvaloniaProperty.Register<MdSearchView, string>(nameof(Query), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<MdSearchView, string>(nameof(Placeholder), "Search");

    public static readonly StyledProperty<bool> IsFullScreenProperty =
        AvaloniaProperty.Register<MdSearchView, bool>(nameof(IsFullScreen));

    public static readonly StyledProperty<System.Collections.IEnumerable?> SuggestionsSourceProperty =
        AvaloniaProperty.Register<MdSearchView, System.Collections.IEnumerable?>(nameof(SuggestionsSource));

    public static readonly StyledProperty<IDataTemplate?> ResultItemTemplateProperty =
        AvaloniaProperty.Register<MdSearchView, IDataTemplate?>(nameof(ResultItemTemplate));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<MdSearchView, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<ICommand?> SearchCommandProperty =
        AvaloniaProperty.Register<MdSearchView, ICommand?>(nameof(SearchCommand));

    private readonly AvaloniaList<MdSearchSuggestion> _suggestionItems = new();
    private readonly List<(string Text, string? Subtitle, string Icon)> _suggestions = [];
    private readonly List<string> _recentQueries = [];
    private readonly MdCollectionObserver _observer;
    private IDisposable? _handle;
    private bool _syncingIsOpen;
    private IInputPane? _inputPane;
    private EventHandler<InputPaneStateEventArgs>? _inputPaneHandler;
    private Action? _refreshCardSize;

    static MdSearchView()
    {
        SuggestionsSourceProperty.Changed.AddClassHandler<MdSearchView>((v, e) =>
        {
            var src = e.GetNewValue<System.Collections.IEnumerable?>();
            v._observer.Attach(src);
            v.ReloadFromSource(src);
        });
        IsOpenProperty.Changed.AddClassHandler<MdSearchView>((v, e) =>
        {
            if (v._syncingIsOpen) return;
            if (e.GetNewValue<bool>())
                v.Show(v.Parent as Visual ?? v);
            else
                v.Close();
        });
    }

    public MdSearchView()
    {
        Placeholder = MdLocalization.Search;
        _observer = new MdCollectionObserver(this, () => ReloadFromSource(SuggestionsSource));
        _suggestionItems.CollectionChanged += (_, _) =>
        {
            foreach (var item in _suggestionItems)
            {
                MdVisualTreeHelpers.AttachLogicalChild(this, item);
                if (item.IsRecent)
                    AddRecent(item.Text);
                else if (!_suggestions.Any(s => string.Equals(s.Text, item.Text, StringComparison.Ordinal)))
                    _suggestions.Add((item.Text, item.Subtitle, item.Icon));
            }
        };
    }

    protected override Size MeasureOverride(Size availableSize) => default;

    [Content]
    public AvaloniaList<MdSearchSuggestion> Suggestions => _suggestionItems;

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

    public MdSearchView AddSuggestion(string text, string? subtitle = null, string icon = MdIcons.Search)
    {
        _suggestions.Add((text, subtitle, icon));
        return this;
    }

    public MdSearchView AddRecent(string query)
    {
        if (!string.IsNullOrWhiteSpace(query) && !_recentQueries.Contains(query, StringComparer.OrdinalIgnoreCase))
            _recentQueries.Insert(0, query);
        return this;
    }

    public void ClearRecentSearches() => _recentQueries.Clear();

    private void ReloadFromSource(System.Collections.IEnumerable? source)
    {
        _suggestions.Clear();
        if (source is null) return;
        foreach (var item in source)
        {
            if (item is not null)
                _suggestions.Add((item.ToString() ?? string.Empty, null, MdIcons.Search));
        }
    }

    public void Show(Visual? anchor = null)
    {
        Close();

        var t = MdTheme.Current;
        var c = t.Color;

        var backBtn = new MdIconButton(MdIcons.ArrowBack);
        backBtn.Clicked += (_, _) => Close();

        var entry = new MdNativeEntry
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

        var clearBtn = new MdIconButton(MdIcons.Close)
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
                resultsStack.Children.Add(new MdListSubheader("Recent searches"));
                foreach (var rec in _recentQueries.Take(5))
                {
                    var captured = rec;
                    var item = new MdListItem(rec, leadingIcon: MdIcons.History, trailingIcon: MdIcons.NorthWest);
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
                resultsStack.Children.Add(new MdListSubheader(string.IsNullOrEmpty(trimmed) ? "Suggestions" : "Results"));
                foreach (var (text, sub, icon) in filtered)
                {
                    var captured = text;
                    if (MdVisualTreeHelpers.BuildTemplateControl(captured, ResultItemTemplate) is { } templated)
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
                        var item = new MdListItem(text, sub, leadingIcon: icon);
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
                var emptyLbl = new MdLabel($"No results for \"{trimmed}\"", MdTypeRole.BodyMedium)
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
        rootGrid.Add(new MdDivider(), 0, 1);
        rootGrid.Add(new MdScrollViewer { Content = resultsStack }, 0, 2);

        var effectiveAnchor = anchor ?? (this.Parent as Visual) ?? this;
        var topLevel = MdOverlay.ResolveTopLevel(effectiveAnchor);

        var card = new MdSurface
        {
            Radius = IsFullScreen ? 0 : 28,
            Elevation = 3,
            FillColor = c.SurfaceContainerHigh,
            HorizontalAlignment = IsFullScreen ? HorizontalAlignment.Stretch : HorizontalAlignment.Center,
            VerticalAlignment = IsFullScreen ? VerticalAlignment.Stretch : VerticalAlignment.Top,
            Content = IsFullScreen ? new MdSafeArea(rootGrid, MdSafeAreaEdges.All) : rootGrid,
        };

        // The card caps depend on the overlay size, the safe area and the soft keyboard,
        // all of which can change while the search view is open (resize, rotation, IME).
        void RefreshCardSize()
        {
            var current = MdOverlay.GetOverlaySize(effectiveAnchor);
            var insets = MdOverlay.SafeAreaOf(effectiveAnchor);
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
        handle = MdOverlay.Show(
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

        MdMotion.ScaleIn(card, 0.94, MdMotion.Medium1);
        Dispatcher.UIThread.Post(() => entry.Focus(), DispatcherPriority.Input);
    }

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
