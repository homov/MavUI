using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Mav.Controls;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Platform;

/// <summary>
/// Chromeless single-line <see cref="TextBox"/> used inside Material 3 text fields,
/// search bars, combo boxes, pin inputs, and date/time pickers so the enclosing
/// Material container owns the border, background, and state indicators.
/// </summary>
public class MavNativeEntry : TextBox
{
    private static readonly IControlTemplate ChromelessEntryTemplate =
        new FuncControlTemplate<TextBox>((box, scope) =>
        {
            var placeholder = new TextBlock
            {
                Name = "PART_Placeholder",
                IsHitTestVisible = false,
                [!TextBlock.TextProperty] = new TemplateBinding(PlaceholderTextProperty),
                [!TextBlock.ForegroundProperty] = new TemplateBinding(PlaceholderForegroundProperty),
                [!TextBlock.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
                [!TextBlock.TextWrappingProperty] = new TemplateBinding(TextWrappingProperty),
                [!Layoutable.HorizontalAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [!Layoutable.VerticalAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty),
            };
            placeholder.Bind(
                Visual.IsVisibleProperty,
                box.GetBindingObservable(TextProperty, static text => string.IsNullOrEmpty(text)));

            var presenter = new TextPresenter
            {
                Name = "PART_TextPresenter",
                [!TextPresenter.TextProperty] = new TemplateBinding(TextProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.CaretBlinkIntervalProperty] = new TemplateBinding(CaretBlinkIntervalProperty),
                [!TextPresenter.CaretIndexProperty] = new TemplateBinding(CaretIndexProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionStartProperty] = new TemplateBinding(SelectionStartProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionEndProperty] = new TemplateBinding(SelectionEndProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
                [!TextPresenter.TextWrappingProperty] = new TemplateBinding(TextWrappingProperty),
                [!TextPresenter.LineHeightProperty] = new TemplateBinding(LineHeightProperty),
                [!TextPresenter.LetterSpacingProperty] = new TemplateBinding(LetterSpacingProperty),
                [!TextPresenter.PasswordCharProperty] = new TemplateBinding(PasswordCharProperty),
                [!TextPresenter.RevealPasswordProperty] = new TemplateBinding(RevealPasswordProperty),
                [!TextPresenter.SelectionBrushProperty] = new TemplateBinding(SelectionBrushProperty),
                [!TextPresenter.SelectionForegroundBrushProperty] = new TemplateBinding(SelectionForegroundBrushProperty),
                [!TextPresenter.CaretBrushProperty] = new TemplateBinding(CaretBrushProperty),
                [!Layoutable.HorizontalAlignmentProperty] = new TemplateBinding(HorizontalContentAlignmentProperty),
                [!Layoutable.VerticalAlignmentProperty] = new TemplateBinding(VerticalContentAlignmentProperty),
            };
            scope.Register("PART_TextPresenter", presenter);

            var panel = new Panel
            {
                Background = Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Ibeam),
            };
            panel.Children.Add(placeholder);
            panel.Children.Add(presenter);

            var scroll = new MavScrollViewer
            {
                Name = "PART_ScrollViewer",
                HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsScrollChainingEnabled = true,
                Content = panel,
            };
            scope.Register("PART_ScrollViewer", scroll);
            return scroll;
        });

    public event EventHandler? Completed;

    /// <summary>Creates an entry that uses the native text control on the current platform.</summary>
    public MavNativeEntry()
    {
        Theme = null;
        Template = ChromelessEntryTemplate;
        Text = string.Empty;
        Background = Brushes.Transparent;
        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        MinHeight = 0;
        MinWidth = 0;
        FocusAdorner = null;
        AcceptsReturn = false;
        TextWrapping = TextWrapping.NoWrap;
        VerticalAlignment = VerticalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Center;
        FontFamily = MavFonts.RobotoFont;

        MavTextContextMenu.Attach(this);
    }

    public Color TextColor
    {
        get => (Foreground as ISolidColorBrush)?.Color ?? Colors.Black;
        set
        {
            var primary = MavTheme.Current.Color.Primary;
            var fgBrush = MavTheme.BrushOf(value);
            Foreground = fgBrush;
            SelectionForegroundBrush = fgBrush;
            CaretBrush = MavTheme.BrushOf(primary);
            SelectionBrush = MavTheme.BrushOf(primary.WithAlpha(0.30f));
        }
    }

    public string? Placeholder
    {
        get => PlaceholderText;
        set => PlaceholderText = value;
    }

    public Color PlaceholderColor
    {
        get => (PlaceholderForeground as ISolidColorBrush)?.Color ?? Colors.Gray;
        set => PlaceholderForeground = MavTheme.BrushOf(value);
    }

    public TextAlignment HorizontalTextAlignment
    {
        get => TextAlignment;
        set => TextAlignment = value;
    }

    public bool IsPassword
    {
        get => PasswordChar != '\0';
        set => PasswordChar = value ? '\u2022' : '\0';
    }

    public int CursorPosition
    {
        get => CaretIndex;
        set => CaretIndex = Math.Max(0, value);
    }

    public int SelectionLength
    {
        get => Math.Abs(SelectionEnd - SelectionStart);
        set => SelectionEnd = SelectionStart + Math.Max(0, value);
    }

    /// <summary>Removes focus from the native control.</summary>
    public void Unfocus()
    {
        if (IsFocused)
            TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
    }

    /// <summary>Forwards the press to the native control and keeps focus on it.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (IsEnabled && !IsFocused && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            Focus();
        base.OnPointerPressed(e);
    }

    /// <summary>Opens the context menu on a long press.</summary>
    protected override void OnHolding(HoldingRoutedEventArgs e)
    {
        if (Text is null)
            SetCurrentValue(TextProperty, string.Empty);
        if (IsEnabled && !IsFocused)
            Focus();
        base.OnHolding(e);
    }

    /// <summary>Forwards key presses to the native control.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!AcceptsReturn && e.Key == Key.Enter)
        {
            Completed?.Invoke(this, EventArgs.Empty);
        }
    }
}
