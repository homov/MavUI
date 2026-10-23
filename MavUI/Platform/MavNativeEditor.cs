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
/// Chromeless multi-line <see cref="TextBox"/> used inside <c>MavTextArea</c>.
/// </summary>
public class MavNativeEditor : TextBox
{
    private static readonly IControlTemplate ChromelessEditorTemplate =
        new FuncControlTemplate<TextBox>((box, scope) =>
        {
            var placeholder = new TextBlock
            {
                Name = "PART_Placeholder",
                IsHitTestVisible = false,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Top,
                [!TextBlock.TextProperty] = new TemplateBinding(PlaceholderTextProperty),
                [!TextBlock.ForegroundProperty] = new TemplateBinding(PlaceholderForegroundProperty),
                [!TextBlock.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
            };
            placeholder.Bind(
                Visual.IsVisibleProperty,
                box.GetBindingObservable(TextProperty, static text => string.IsNullOrEmpty(text)));

            var presenter = new TextPresenter
            {
                Name = "PART_TextPresenter",
                VerticalAlignment = VerticalAlignment.Top,
                [!TextPresenter.TextProperty] = new TemplateBinding(TextProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.CaretBlinkIntervalProperty] = new TemplateBinding(CaretBlinkIntervalProperty),
                [!TextPresenter.CaretIndexProperty] = new TemplateBinding(CaretIndexProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionStartProperty] = new TemplateBinding(SelectionStartProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.SelectionEndProperty] = new TemplateBinding(SelectionEndProperty) { Mode = BindingMode.TwoWay },
                [!TextPresenter.TextAlignmentProperty] = new TemplateBinding(TextAlignmentProperty),
                [!TextPresenter.TextWrappingProperty] = new TemplateBinding(TextWrappingProperty),
                [!TextPresenter.LineHeightProperty] = new TemplateBinding(LineHeightProperty),
                [!TextPresenter.LetterSpacingProperty] = new TemplateBinding(LetterSpacingProperty),
                [!TextPresenter.SelectionBrushProperty] = new TemplateBinding(SelectionBrushProperty),
                [!TextPresenter.SelectionForegroundBrushProperty] = new TemplateBinding(SelectionForegroundBrushProperty),
                [!TextPresenter.CaretBrushProperty] = new TemplateBinding(CaretBrushProperty),
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
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsScrollChainingEnabled = true,
                Child = panel,
            };
            scope.Register("PART_ScrollViewer", scroll);
            return scroll;
        });

    /// <summary>Creates a multi-line editor that uses the native text control on the current platform.</summary>
    public MavNativeEditor()
    {
        Theme = null;
        Template = ChromelessEditorTemplate;
        Text = string.Empty;
        Background = Brushes.Transparent;
        BorderBrush = Brushes.Transparent;
        BorderThickness = new Thickness(0);
        Padding = new Thickness(0);
        MinHeight = 0;
        MinWidth = 0;
        FocusAdorner = null;
        AcceptsReturn = true;
        TextWrapping = TextWrapping.Wrap;
        VerticalAlignment = VerticalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Top;
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
}
