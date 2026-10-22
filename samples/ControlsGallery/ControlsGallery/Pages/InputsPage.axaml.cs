using Avalonia.Controls;
using Mav;
using Mav.Controls;

namespace Mav.ControlsGallery.Pages;

public partial class InputsPage : UserControl
{
    private readonly MavFormValidator _validator = new() { ValidateOnChange = true };

    public InputsPage()
    {
        InitializeComponent();

        TagInput.AddTag("material").AddTag("avalonia");
        SearchBar.AttachSearchView(new[] { "MavButton", "MavCard", "MavDataTable", "MavDialog" });

        _validator.Field(NameField).Required().MinLength(2);
        _validator.Field(EmailField).Required().Email();
        _validator.Field(CityField).Required();

        SubmitButton.Clicked += (_, _) =>
        {
            var valid = _validator.Validate();
            StatusLabel.Text = valid
                ? MavLocalization.Get("Inputs.Form.Valid", "All fields are valid.")
                : string.Join(" · ", _validator.Errors);
        };
    }
}
