using System.Globalization;
using System.Resources;
using Avalonia;
using Avalonia.Controls;

namespace Mav;

/// <summary>Selects which property an attached localization key updates.</summary>
public enum MavLocalizationTarget
{
    /// <summary>Pick the natural text property for the control type.</summary>
    Auto,
    /// <summary><see cref="TextBlock.Text"/> or <see cref="Controls.MavButton.Text"/>.</summary>
    Text,
    /// <summary><see cref="Controls.MavTextField.Label"/> or <see cref="Controls.MavSelect.Label"/>.</summary>
    Label,
    /// <summary><see cref="Controls.MavTextField.Placeholder"/> or <see cref="Controls.MavSearchBar.Placeholder"/>.</summary>
    Placeholder,
    /// <summary><see cref="Window.Title"/> or <see cref="Controls.MavDialog.Title"/>.</summary>
    Title,
    /// <summary>The control's tooltip.</summary>
    Tooltip,
}

/// <summary>
/// Pluggable localization provider for Material Design 3 built-in strings
/// (date/time picker dialogs, tooltips, accessibility labels, action buttons).
/// Return <see langword="null"/> or an empty string when a key is not available;
/// <see cref="MavLocalization.Get"/> then uses its fallback text.
/// </summary>
public interface IMavLocalizationProvider
{
    string? GetString(string key, CultureInfo? culture = null);
}

/// <summary>
/// Global localization entry point. Set <see cref="Current"/> once at startup,
/// optionally call <see cref="SetCulture"/>, and use <see cref="Observe"/> or the
/// <c>Localize</c> extension methods for application strings. Observers refresh
/// automatically, so consumers never subscribe to localization events.
/// AXAML can use the attached <see cref="KeyProperty"/> instead of code-behind.
/// </summary>
public static class MavLocalization
{
    private static IMavLocalizationProvider _provider = DefaultProvider.Instance;
    private static int _version;

    /// <summary>Attached AXAML key for the text to localize.</summary>
    public static readonly AttachedProperty<string?> KeyProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Key", typeof(MavLocalization));

    /// <summary>Attached AXAML fallback used when the provider has no value.</summary>
    public static readonly AttachedProperty<string?> FallbackProperty =
        AvaloniaProperty.RegisterAttached<Control, string?>("Fallback", typeof(MavLocalization));

    /// <summary>Attached AXAML target property. Defaults to <see cref="MavLocalizationTarget.Auto"/>.</summary>
    public static readonly AttachedProperty<MavLocalizationTarget> TargetProperty =
        AvaloniaProperty.RegisterAttached<Control, MavLocalizationTarget>("Target", typeof(MavLocalization));

    private static readonly AttachedProperty<bool> ObservedProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("Observed", typeof(MavLocalization));

    static MavLocalization()
    {
        KeyProperty.Changed.AddClassHandler<Control>((control, _) => OnLocalizationChanged(control));
        FallbackProperty.Changed.AddClassHandler<Control>((control, _) => OnLocalizationChanged(control));
        TargetProperty.Changed.AddClassHandler<Control>((control, _) => OnLocalizationChanged(control));
    }

    public static string? GetKey(Control control) => control.GetValue(KeyProperty);
    public static void SetKey(Control control, string? value) => control.SetValue(KeyProperty, value);
    public static string? GetFallback(Control control) => control.GetValue(FallbackProperty);
    public static void SetFallback(Control control, string? value) => control.SetValue(FallbackProperty, value);
    public static MavLocalizationTarget GetTarget(Control control) => control.GetValue(TargetProperty);
    public static void SetTarget(Control control, MavLocalizationTarget value) => control.SetValue(TargetProperty, value);

    private static void OnLocalizationChanged(Control control)
    {
        if (!control.GetValue(ObservedProperty))
        {
            control.SetValue(ObservedProperty, true);
            Observe(control, ApplyAttachedText);
        }
        else
        {
            ApplyAttachedText(control);
        }
    }

    private static void ApplyAttachedText(Control control)
    {
        var key = GetKey(control);
        if (string.IsNullOrEmpty(key))
            return;

        var text = Get(key, GetFallback(control) ?? key);
        switch (GetTarget(control))
        {
            case MavLocalizationTarget.Text:
                SetText(control, text);
                break;
            case MavLocalizationTarget.Label:
                if (control is Controls.MavTextField field)
                    field.Label = text;
                else if (control is Controls.MavSelect select)
                    select.Label = text;
                break;
            case MavLocalizationTarget.Placeholder:
                if (control is Controls.MavTextField placeholderField)
                    placeholderField.Placeholder = text;
                else if (control is Controls.MavSearchBar searchBar)
                    searchBar.Placeholder = text;
                break;
            case MavLocalizationTarget.Title:
                if (control is Window window)
                    window.Title = text;
                else if (control is Controls.MavDialog dialog)
                    dialog.Title = text;
                break;
            case MavLocalizationTarget.Tooltip:
                ToolTip.SetTip(control, text);
                break;
            default:
                AutoApplyText(control, text);
                break;
        }
    }

    private static void AutoApplyText(Control control, string text)
    {
        switch (control)
        {
            case TextBlock textBlock:
                textBlock.Text = text;
                break;
            case Controls.MavButton button:
                button.Text = text;
                break;
            case Window window:
                window.Title = text;
                break;
            case Controls.MavDialog dialog:
                dialog.Title = text;
                break;
            case Controls.MavTextField field:
                field.Label = text;
                break;
            case Controls.MavSelect select:
                select.Label = text;
                break;
            case Controls.MavSearchBar searchBar:
                searchBar.Placeholder = text;
                break;
            default:
                throw new InvalidOperationException(
                    $"MavLocalization.Key is not supported on {control.GetType().Name}. " +
                    "Set MavLocalization.Target or use MavLocalization.Observe in code.");
        }
    }

    private static void SetText(Control control, string text)
    {
        switch (control)
        {
            case TextBlock textBlock:
                textBlock.Text = text;
                break;
            case Controls.MavButton button:
                button.Text = text;
                break;
            default:
                throw new InvalidOperationException(
                    $"MavLocalization.Target=Text requires TextBlock or MavButton, not {control.GetType().Name}.");
        }
    }

    /// <summary>Raised after the provider or the current UI culture changes.</summary>
    public static event EventHandler? Changed;

    /// <summary>The active provider. Setting it refreshes all observed texts.</summary>
    public static IMavLocalizationProvider Current
    {
        get => _provider;
        set
        {
            _provider = value ?? DefaultProvider.Instance;
            NotifyChanged();
        }
    }

    /// <summary>Alias for <see cref="Current"/>.</summary>
    public static IMavLocalizationProvider Provider
    {
        get => Current;
        set => Current = value;
    }

    /// <summary>
    /// The culture used for built-in strings. Setting it changes the UI culture
    /// (and, by default, the formatting culture) and refreshes observed texts.
    /// </summary>
    public static CultureInfo Culture
    {
        get => CultureInfo.CurrentUICulture;
        set => SetCulture(value);
    }

    /// <summary>
    /// Switches the application language. No subscriptions are required:
    /// texts attached with <see cref="Observe"/> or <c>Localize</c> update themselves.
    /// </summary>
    /// <param name="culture">The new UI culture.</param>
    /// <param name="setFormattingCulture">
    /// Also use <paramref name="culture"/> for numbers, dates and currency.
    /// Pass <see langword="false"/> to keep the formatting culture unchanged.
    /// </param>
    public static void SetCulture(CultureInfo culture, bool setFormattingCulture = true)
    {
        ArgumentNullException.ThrowIfNull(culture);

        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentUICulture = culture;

        if (setFormattingCulture)
        {
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.CurrentCulture = culture;
        }

        NotifyChanged();
    }

    /// <summary>
    /// Re-reads strings from the current provider and refreshes observed texts.
    /// Call this if the provider's data changed in place, or if the culture was
    /// changed directly through <see cref="CultureInfo"/> instead of
    /// <see cref="SetCulture"/>.
    /// </summary>
    public static void Refresh() => NotifyChanged();

    /// <summary>
    /// Returns the string for <paramref name="key"/>. If the provider has no value,
    /// <paramref name="fallback"/> is used; with no fallback the key itself is returned.
    /// </summary>
    public static string Get(string key, string fallback = "")
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("A localization key is required.", nameof(key));

        var value = _provider.GetString(key, CultureInfo.CurrentUICulture);
        if (!string.IsNullOrEmpty(value))
            return value;

        return string.IsNullOrEmpty(fallback) ? key : fallback;
    }

    /// <summary>
    /// Returns a formatted localized string, e.g. <c>Format("Rows", "{0} rows", count)</c>.
    /// </summary>
    public static string Format(string key, string fallback, params object?[] args)
    {
        var value = Get(key, fallback);
        return args.Length == 0 ? value : string.Format(CultureInfo.CurrentCulture, value, args);
    }

    /// <summary>
    /// Attaches a localization observer to <paramref name="view"/>. <paramref name="apply"/>
    /// runs immediately and again whenever the provider or UI culture changes while the
    /// view is attached to the visual tree. The observer detaches automatically.
    /// </summary>
    public static void Observe<T>(T view, Action<T> apply) where T : Control
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(apply);

        var appliedVersion = -1;
        var subscribed = false;

        void Invoke()
        {
            appliedVersion = _version;
            apply(view);
        }

        void OnChanged(object? sender, EventArgs e)
        {
            appliedVersion = _version;
            apply(view);
        }

        Invoke();

        view.AttachedToVisualTree += (_, _) =>
        {
            if (!subscribed)
            {
                Changed += OnChanged;
                subscribed = true;
            }

            if (appliedVersion != _version)
                Invoke();
        };

        view.DetachedFromVisualTree += (_, _) =>
        {
            if (!subscribed)
                return;

            Changed -= OnChanged;
            subscribed = false;
        };

        if (view.IsLoaded || TopLevel.GetTopLevel(view) is not null)
        {
            Changed += OnChanged;
            subscribed = true;
        }
    }

    private static void NotifyChanged()
    {
        _version++;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    // Date/time picker
    public static string SelectDate => Get(Keys.SelectDate, "Select date");
    public static string SelectDateRange => Get(Keys.SelectDateRange, "Select range");
    public static string EnterDate => Get(Keys.EnterDate, "Enter date");
    public static string EnterDateRange => Get(Keys.EnterDateRange, "Enter dates");
    public static string SwitchToInput => Get(Keys.SwitchToInput, "Switch to text input");
    public static string SwitchToCalendar => Get(Keys.SwitchToCalendar, "Switch to calendar");
    public static string StartDate => Get(Keys.StartDate, "Start date");
    public static string EndDate => Get(Keys.EndDate, "End date");
    public static string DateRangeSeparator => Get(Keys.DateRangeSeparator, "\u2013");
    public static string InvalidDate => Get(Keys.InvalidDate, "Invalid date format");
    public static string InvalidRange => Get(Keys.InvalidRange, "End date must be on or after start date");
    public static string DateFieldHint => Get(Keys.DateFieldHint, "yyyy-mm-dd");
    public static string PreviousMonth => Get(Keys.PreviousMonth, "Previous month");
    public static string NextMonth => Get(Keys.NextMonth, "Next month");
    public static string SelectYear => Get(Keys.SelectYear, "Select year");

    // Time picker
    public static string SelectTime => Get(Keys.SelectTime, "Select time");
    public static string EnterTime => Get(Keys.EnterTime, "Enter time");
    public static string Hour => Get(Keys.Hour, "Hour");
    public static string Minute => Get(Keys.Minute, "Minute");
    public static string Am => Get(Keys.Am, "AM");
    public static string Pm => Get(Keys.Pm, "PM");
    public static string SwitchToClock => Get(Keys.SwitchToClock, "Switch to clock dial");
    public static string SwitchToTextInput => Get(Keys.SwitchToTextInput, "Switch to text input");

    // Common control labels
    public static string Ok => Get(Keys.Ok, "OK");
    public static string Cancel => Get(Keys.Cancel, "Cancel");
    public static string Close => Get(Keys.Close, "Close");
    public static string Back => Get(Keys.Back, "Back");
    public static string Clear => Get(Keys.Clear, "Clear");
    public static string Search => Get(Keys.Search, "Search");
    public static string Select => Get(Keys.Select, "Select");
    public static string ShowPassword => Get(Keys.ShowPassword, "Show password");
    public static string HidePassword => Get(Keys.HidePassword, "Hide password");
    public static string OpenMenu => Get(Keys.OpenMenu, "Open menu");
    public static string CloseDrawer => Get(Keys.CloseDrawer, "Close navigation drawer");
    public static string MoreOptions => Get(Keys.MoreOptions, "More options");
    public static string Expand => Get(Keys.Expand, "Expand");
    public static string Collapse => Get(Keys.Collapse, "Collapse");
    public static string Delete => Get(Keys.Delete, "Delete");
    public static string Dismiss => Get(Keys.Dismiss, "Dismiss");
    public static string Undo => Get(Keys.Undo, "Undo");
    public static string Redo => Get(Keys.Redo, "Redo");
    public static string Cut => Get(Keys.Cut, "Cut");
    public static string Copy => Get(Keys.Copy, "Copy");
    public static string Paste => Get(Keys.Paste, "Paste");
    public static string SelectAll => Get(Keys.SelectAll, "Select all");
    public static string Retry => Get(Keys.Retry, "Retry");

    // Collections and pagination
    public static string ClearSelection => Get(Keys.ClearSelection, "Clear selection");
    public static string PreviousPage => Get(Keys.PreviousPage, "Previous page");
    public static string NextPage => Get(Keys.NextPage, "Next page");
    public static string SelectRow => Get(Keys.SelectRow, "Select row");
    public static string SelectedCountFormat => Get(Keys.SelectedCountFormat, "{0} selected");
    public static string PageRangeFormat => Get(Keys.PageRangeFormat, "{0}\u2013{1} of {2}");
    public static string EmptyPageRange => Get(Keys.EmptyPageRange, "0 of 0");

    /// <summary>Stable keys for all built-in strings.</summary>
    public static class Keys
    {
        // Common
        public const string Ok = "Ok";
        public const string Cancel = "Cancel";
        public const string Close = "Close";
        public const string Back = "Back";
        public const string Clear = "Clear";
        public const string Search = "Search";
        public const string Select = "Select";

        // Date picker
        public const string SelectDate = "SelectDate";
        public const string SelectDateRange = "SelectDateRange";
        public const string EnterDate = "EnterDate";
        public const string EnterDateRange = "EnterDateRange";
        public const string SwitchToInput = "SwitchToInput";
        public const string SwitchToCalendar = "SwitchToCalendar";
        public const string StartDate = "StartDate";
        public const string EndDate = "EndDate";
        public const string DateRangeSeparator = "DateRangeSeparator";
        public const string InvalidDate = "InvalidDate";
        public const string InvalidRange = "InvalidRange";
        public const string DateFieldHint = "DateFieldHint";
        public const string PreviousMonth = "PreviousMonth";
        public const string NextMonth = "NextMonth";
        public const string SelectYear = "SelectYear";

        // Time picker
        public const string SelectTime = "SelectTime";
        public const string EnterTime = "EnterTime";
        public const string Hour = "Hour";
        public const string Minute = "Minute";
        public const string Am = "Am";
        public const string Pm = "Pm";
        public const string SwitchToClock = "SwitchToClock";
        public const string SwitchToTextInput = "SwitchToTextInput";

        // Controls
        public const string ShowPassword = "ShowPassword";
        public const string HidePassword = "HidePassword";
        public const string OpenMenu = "OpenMenu";
        public const string CloseDrawer = "CloseDrawer";
        public const string MoreOptions = "MoreOptions";
        public const string Expand = "Expand";
        public const string Collapse = "Collapse";
        public const string Delete = "Delete";
        public const string Dismiss = "Dismiss";
        public const string Undo = "Undo";
        public const string Redo = "Redo";
        public const string Cut = "Cut";
        public const string Copy = "Copy";
        public const string Paste = "Paste";
        public const string SelectAll = "SelectAll";
        public const string Retry = "Retry";

        // Collections and pagination
        public const string ClearSelection = "ClearSelection";
        public const string PreviousPage = "PreviousPage";
        public const string NextPage = "NextPage";
        public const string SelectRow = "SelectRow";
        public const string SelectedCountFormat = "SelectedCountFormat";
        public const string PageRangeFormat = "PageRangeFormat";
        public const string EmptyPageRange = "EmptyPageRange";
    }

    /// <summary>English defaults used when an app provider has no entry.</summary>
    public sealed class DefaultProvider : IMavLocalizationProvider
    {
        public static DefaultProvider Instance { get; } = new();

        private static readonly Dictionary<string, string> Defaults = new(StringComparer.OrdinalIgnoreCase)
        {
            [Keys.Ok] = "OK",
            [Keys.Cancel] = "Cancel",
            [Keys.Close] = "Close",
            [Keys.Back] = "Back",
            [Keys.Clear] = "Clear",
            [Keys.Search] = "Search",
            [Keys.Select] = "Select",
            [Keys.SelectDate] = "Select date",
            [Keys.SelectDateRange] = "Select range",
            [Keys.EnterDate] = "Enter date",
            [Keys.EnterDateRange] = "Enter dates",
            [Keys.SwitchToInput] = "Switch to text input",
            [Keys.SwitchToCalendar] = "Switch to calendar",
            [Keys.StartDate] = "Start date",
            [Keys.EndDate] = "End date",
            [Keys.DateRangeSeparator] = "\u2013",
            [Keys.InvalidDate] = "Invalid date format",
            [Keys.InvalidRange] = "End date must be on or after start date",
            [Keys.DateFieldHint] = "yyyy-mm-dd",
            [Keys.PreviousMonth] = "Previous month",
            [Keys.NextMonth] = "Next month",
            [Keys.SelectYear] = "Select year",
            [Keys.SelectTime] = "Select time",
            [Keys.EnterTime] = "Enter time",
            [Keys.Hour] = "Hour",
            [Keys.Minute] = "Minute",
            [Keys.Am] = "AM",
            [Keys.Pm] = "PM",
            [Keys.SwitchToClock] = "Switch to clock dial",
            [Keys.SwitchToTextInput] = "Switch to text input",
            [Keys.ShowPassword] = "Show password",
            [Keys.HidePassword] = "Hide password",
            [Keys.OpenMenu] = "Open menu",
            [Keys.CloseDrawer] = "Close navigation drawer",
            [Keys.MoreOptions] = "More options",
            [Keys.Expand] = "Expand",
            [Keys.Collapse] = "Collapse",
            [Keys.Delete] = "Delete",
            [Keys.Dismiss] = "Dismiss",
            [Keys.Undo] = "Undo",
            [Keys.Redo] = "Redo",
            [Keys.Cut] = "Cut",
            [Keys.Copy] = "Copy",
            [Keys.Paste] = "Paste",
            [Keys.SelectAll] = "Select all",
            [Keys.Retry] = "Retry",
            [Keys.ClearSelection] = "Clear selection",
            [Keys.PreviousPage] = "Previous page",
            [Keys.NextPage] = "Next page",
            [Keys.SelectRow] = "Select row",
            [Keys.SelectedCountFormat] = "{0} selected",
            [Keys.PageRangeFormat] = "{0}\u2013{1} of {2}",
            [Keys.EmptyPageRange] = "0 of 0",
        };

        public string? GetString(string key, CultureInfo? culture = null) =>
            Defaults.TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Localization provider backed by a dictionary. Missing keys fall through to
    /// <paramref name="fallback"/> (English built-ins by default).
    /// </summary>
    public sealed class DictionaryProvider : IMavLocalizationProvider
    {
        private readonly Dictionary<string, string> _values;
        private readonly IMavLocalizationProvider _fallback;

        public DictionaryProvider(
            IDictionary<string, string> values,
            IMavLocalizationProvider? fallback = null)
        {
            ArgumentNullException.ThrowIfNull(values);
            _values = new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase);
            _fallback = fallback ?? DefaultProvider.Instance;
        }

        public string? GetString(string key, CultureInfo? culture = null)
        {
            if (_values.TryGetValue(key, out var value) && !string.IsNullOrEmpty(value))
                return value;

            return _fallback.GetString(key, culture);
        }
    }

    /// <summary>
    /// Localization provider backed by a .NET <see cref="ResourceManager"/>
    /// (generated .resx resources). Missing keys fall through to English built-ins
    /// or a custom <paramref name="fallback"/> provider.
    /// </summary>
    public sealed class ResourceManagerProvider : IMavLocalizationProvider
    {
        private readonly ResourceManager _resources;
        private readonly IMavLocalizationProvider _fallback;

        public ResourceManagerProvider(
            ResourceManager resources,
            IMavLocalizationProvider? fallback = null)
        {
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _fallback = fallback ?? DefaultProvider.Instance;
        }

        public ResourceManagerProvider(
            Type resourceSource,
            IMavLocalizationProvider? fallback = null)
            : this(new ResourceManager(resourceSource), fallback)
        {
        }

        public string? GetString(string key, CultureInfo? culture = null)
        {
            var value = _resources.GetString(key, culture ?? CultureInfo.CurrentUICulture);
            return string.IsNullOrEmpty(value) ? _fallback.GetString(key, culture) : value;
        }
    }

    /// <summary>Queries several providers in order and returns the first non-empty value.</summary>
    public sealed class ChainedProvider : IMavLocalizationProvider
    {
        private readonly IMavLocalizationProvider[] _providers;

        public ChainedProvider(params IMavLocalizationProvider[] providers)
        {
            ArgumentNullException.ThrowIfNull(providers);
            if (providers.Length == 0)
                throw new ArgumentException("At least one provider is required.", nameof(providers));

            _providers = providers;
        }

        public string? GetString(string key, CultureInfo? culture = null)
        {
            foreach (var provider in _providers)
            {
                var value = provider.GetString(key, culture);
                if (!string.IsNullOrEmpty(value))
                    return value;
            }

            return null;
        }
    }

    /// <summary>Delegate-based provider, convenient for tiny apps and tests.</summary>
    public sealed class DelegateProvider : IMavLocalizationProvider
    {
        private readonly Func<string, CultureInfo?, string?> _getString;

        public DelegateProvider(Func<string, CultureInfo?, string?> getString)
        {
            _getString = getString ?? throw new ArgumentNullException(nameof(getString));
        }

        public DelegateProvider(Func<string, string?> getString)
            : this((key, _) => getString(key))
        {
            ArgumentNullException.ThrowIfNull(getString);
        }

        public string? GetString(string key, CultureInfo? culture = null) =>
            _getString(key, culture);
    }
}
