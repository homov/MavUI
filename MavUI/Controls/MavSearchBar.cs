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
/// Material Design 3 pill Search Bar (56dp height, SurfaceContainerHigh, leading search icon,
/// clear button, and optional trailing avatar or icon).
/// </summary>
public class MavSearchBar : MavContentView
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<MavSearchBar, string>(nameof(Text), string.Empty, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<MavSearchBar, string>(nameof(Placeholder), MavLocalization.Search);

    public static readonly StyledProperty<string?> LeadingIconProperty =
        AvaloniaProperty.Register<MavSearchBar, string?>(nameof(LeadingIcon), MavIcons.Search);

    public static readonly StyledProperty<string?> TrailingIconProperty =
        AvaloniaProperty.Register<MavSearchBar, string?>(nameof(TrailingIcon));

    public static readonly StyledProperty<Control?> TrailingViewProperty =
        AvaloniaProperty.Register<MavSearchBar, Control?>(nameof(TrailingView));

    public static readonly StyledProperty<ICommand?> SearchCommandProperty =
        AvaloniaProperty.Register<MavSearchBar, ICommand?>(nameof(SearchCommand));

    public static readonly StyledProperty<object?> SearchCommandParameterProperty =
        AvaloniaProperty.Register<MavSearchBar, object?>(nameof(SearchCommandParameter));

    public static readonly StyledProperty<ICommand?> LeadingIconCommandProperty =
        AvaloniaProperty.Register<MavSearchBar, ICommand?>(nameof(LeadingIconCommand));

    public static readonly StyledProperty<ICommand?> TrailingIconCommandProperty =
        AvaloniaProperty.Register<MavSearchBar, ICommand?>(nameof(TrailingIconCommand));

    private readonly MavSurface _pill;
    private readonly MavIcon _leadIcon;
    private readonly MavNativeEntry _entry;
    private readonly Border _clearHit;
    private readonly MavIcon _clearIcon;
    private readonly Border _trailHit;
    private readonly MavIcon _trailIcon;
    private Control? _trailingView;
    private bool _updating;

    public event EventHandler<string>? TextChanged;
    public event EventHandler<string>? SearchSubmitted;
    public event EventHandler? LeadingIconClicked;
    public event EventHandler? TrailingIconClicked;

    static MavSearchBar()
    {
        TextProperty.Changed.AddClassHandler<MavSearchBar>((s, e) => s.OnOuterTextChanged(e.GetNewValue<string>()));
        PlaceholderProperty.Changed.AddClassHandler<MavSearchBar>((s, e) => s._entry.Placeholder = e.GetNewValue<string>());
        LeadingIconProperty.Changed.AddClassHandler<MavSearchBar>((s, e) => s._leadIcon.Glyph = e.GetNewValue<string?>());
        TrailingIconProperty.Changed.AddClassHandler<MavSearchBar>((s, _) => s.SyncTrailing());
        TrailingViewProperty.Changed.AddClassHandler<MavSearchBar>((s, e) =>
        {
            s._trailingView = e.GetNewValue<Control?>();
            MavVisualTreeHelpers.DetachFromParent(s._trailingView);
            s.SyncTrailing();
        });
    }

    /// <summary>Creates a search bar with the localized default placeholder.</summary>
    public MavSearchBar() : this(MavLocalization.Search, null)
    {
    }

    /// <summary>Creates a search bar with the given placeholder and trailing icon.</summary>
    public MavSearchBar(string? placeholder = null, string? trailingIcon = null)
    {
        placeholder ??= MavLocalization.Search;
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Center;

        _leadIcon = new MavIcon(MavIcons.Search, 24) { UseThemeDefaultColor = false };
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

        _entry = new MavNativeEntry
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

        _clearIcon = new MavIcon(MavIcons.Close, 20) { UseThemeDefaultColor = false };
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

        _trailIcon = new MavIcon { Size = 22, UseThemeDefaultColor = false };
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

        _pill = new MavSurface
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

        MavTheme.Observe(this, (self, theme) => self.ApplyTheme(theme));
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

    public MavNativeEntry InnerEntry => _entry;

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
                MavVisualTreeHelpers.DetachFromParent(_trailingView);
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
                MavVisualTreeHelpers.DetachFromParent(_trailIcon);
                _trailHit.Child = _trailIcon;
            }
            _trailHit.IsVisible = true;
        }
        else
        {
            _trailHit.IsVisible = false;
        }
    }

    private void ApplyTheme(MavTheme t)
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
    /// Attaches an <see cref="MavSearchView"/> so tapping or focusing this search bar opens the full Material 3 Search View.
    /// </summary>
    public MavSearchView AttachSearchView(IEnumerable<string>? suggestions = null, bool fullScreen = false)
    {
        var view = new MavSearchView
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
