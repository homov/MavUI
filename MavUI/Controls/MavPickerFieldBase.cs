using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Mav.Platform;
using Mav.Primitives;
using Mav.Theming;

namespace Mav.Controls;

/// <summary>
/// Shared base class for read-only picker text fields (<see cref="MavDatePickerField"/>,
/// <see cref="MavDateRangePickerField"/>, <see cref="MavTimePickerField"/>).
/// </summary>
public abstract class MavPickerFieldBase : MavContentView
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string?> SupportingTextProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, string?>(nameof(SupportingText));

    public static readonly StyledProperty<string?> ErrorTextProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, string?>(nameof(ErrorText));

    public static readonly StyledProperty<MavTextFieldVariant> VariantProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, MavTextFieldVariant>(nameof(Variant), MavTextFieldVariant.Outlined);

    public static readonly StyledProperty<bool> IsClearableProperty =
        AvaloniaProperty.Register<MavPickerFieldBase, bool>(nameof(IsClearable));

    protected readonly MavTextField Field;

    static MavPickerFieldBase()
    {
        LabelProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.Label = e.GetNewValue<string>());
        SupportingTextProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.SupportingText = e.GetNewValue<string?>());
        ErrorTextProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.ErrorText = e.GetNewValue<string?>());
        VariantProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, e) => f.Field.Variant = e.GetNewValue<MavTextFieldVariant>());
        IsClearableProperty.Changed.AddClassHandler<MavPickerFieldBase>((f, _) => f.SyncFieldText());
    }

    /// <summary>Creates a picker field with the given label, variant and leading icon.</summary>
    protected MavPickerFieldBase(string label, MavTextFieldVariant variant, string leadingIcon)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;

        Field = new MavTextField(label, variant, leadingIcon: leadingIcon)
        {
            IsReadOnly = true,
            TrailingIcon = MavIcons.ArrowDropDown,
        };
        Field.InnerEntry.IsHitTestVisible = false;
        Field.TrailingIconClicked += async (_, _) =>
        {
            if (!IsEnabled) return;
            if (IsClearable && HasSelectedValue)
            {
                Clear();
                return;
            }
            await OpenPickerAsync();
        };

        var tapOverlay = new Border
        {
            Background = Brushes.Transparent,
            Cursor = new Cursor(StandardCursorType.Hand),
            Margin = new Thickness(0, 0, 44, 0),
        };
        tapOverlay.PointerReleased += async (_, _) =>
        {
            if (!IsEnabled) return;
            await OpenPickerAsync();
        };

        var grid = new Grid();
        grid.Children.Add(Field);
        grid.Children.Add(tapOverlay);
        Child = grid;

        SetValue(LabelProperty, label);
        SetValue(VariantProperty, variant);
    }

    public bool IsClearable
    {
        get => GetValue(IsClearableProperty);
        set => SetValue(IsClearableProperty, value);
    }

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string? SupportingText
    {
        get => GetValue(SupportingTextProperty);
        set => SetValue(SupportingTextProperty, value);
    }

    public string? ErrorText
    {
        get => GetValue(ErrorTextProperty);
        set => SetValue(ErrorTextProperty, value);
    }

    public MavTextFieldVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    protected abstract bool HasSelectedValue { get; }
    /// <summary>
    /// Clears the selected value.
    /// </summary>
    public abstract void Clear();
    /// <summary>
    /// Opens the picker and applies the value the user selects.
    /// </summary>
    protected abstract Task OpenPickerAsync();
    /// <summary>
    /// Returns the text shown in the field for the current value.
    /// </summary>
    protected abstract string FormatDisplayValue();

    /// <summary>Updates the text in the field from the current value.</summary>
    protected void SyncFieldText()
    {
        Field.Text = FormatDisplayValue();
        Field.TrailingIcon = (IsClearable && HasSelectedValue) ? MavIcons.Cancel : MavIcons.ArrowDropDown;
    }
}
