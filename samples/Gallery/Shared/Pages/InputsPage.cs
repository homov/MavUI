using Avalonia.Controls;
using Avalonia;
using Mav;
using Mav.Controls;
using Mav.Theming;

namespace Mav.Gallery;

internal static class InputsPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Text fields", "Outlined and filled fields with icons, password mode, clear button, counters and errors.", TextFields()),
            GalleryUi.Section("Multiline and structured input", "Text areas, PIN/OTP cells, numeric steppers, tags and search.", OtherInputs()),
            GalleryUi.Section("Validation", "MavFormValidator wires rules to fields and writes ErrorText/HasError.", ValidationForm()));
    }

    private static Control TextFields()
    {
        var name = new MavTextField("Name", MavTextFieldVariant.Outlined, "Ada Lovelace", MavIcons.Person);
        var email = new MavTextField("Email", MavTextFieldVariant.Filled, "ada@example.com", MavIcons.Email)
        {
            SupportingText = "We never share it",
        };
        var password = new MavTextField("Password", MavTextFieldVariant.Outlined, "••••••••", MavIcons.Lock)
        {
            IsPassword = true,
            ShowClearButton = true,
        };
        var error = new MavTextField("API key", MavTextFieldVariant.Outlined)
        {
            Text = "abc",
            ErrorText = "The key must be 32 characters",
        };
        var counter = new MavTextField("Code", MavTextFieldVariant.Outlined)
        {
            MaxLength = 12,
            ShowCounter = true,
            Text = "MD3",
        };

        return GalleryUi.Showcase(
            "MavTextField",
            "Single-line field with variants, leading/trailing icons, password reveal, clear button, counter and error state.",
            GalleryUi.Column(name, email, password, error, counter),
            "new MavTextField(label, variant, placeholder, leadingIcon, supportingText) · Text · ErrorText · ContentType · ReturnKeyType");
    }

    private static Control OtherInputs()
    {
        var area = new MavTextArea("Notes", placeholder: "Write a release note…", minHeight: 96)
        {
            Text = "The gallery shows every control.",
            SupportingText = "Markdown is supported by the host app",
        };
        var pin = new MavPinInput { Length = 6, Code = "1234" };
        var number = new MavNumberBox { Label = "Seats", Minimum = 1, Maximum = 20, Value = 4, Suffix = "people" };
        var tags = new MavTagInput("Tags").AddTag("material").AddTag("avalonia");
        var search = new MavSearchBar("Search the gallery");
        search.AttachSearchView(new[] { "MavButton", "MavCard", "MavDataTable", "MavDialog" });

        return GalleryUi.Showcase(
            "MavTextArea, MavPinInput, MavNumberBox, MavTagInput and MavSearchBar",
            "Multiline editor, OTP cells, numeric stepper, tag entry and a search bar with a search view.",
            GalleryUi.Row(
                new MavCard(MavCardVariant.Outlined, area) { Width = 320 },
                new MavCard(MavCardVariant.Outlined, GalleryUi.Column(pin, number)) { Width = 320 },
                new MavCard(MavCardVariant.Outlined, GalleryUi.Column(tags, search)) { Width = 320 }),
            "MavTextArea · MavPinInput · MavNumberBox · MavTagInput · MavSearchBar.AttachSearchView");
    }

    private static Control ValidationForm()
    {
        var name = new MavTextField("Full name", MavTextFieldVariant.Outlined);
        var email = new MavTextField("Email", MavTextFieldVariant.Outlined);
        var city = new MavTextField("City", MavTextFieldVariant.Outlined);
        var status = new MavLabel
        {
            Text = "Submit to validate",
            Role = MavTypeRole.BodySmall,
            Secondary = true,
        };

        var validator = new MavFormValidator { ValidateOnChange = true };
        validator.Field(name).Required().MinLength(2);
        validator.Field(email).Required().Email();
        validator.Field(city).Required();

        var submit = new MavButton("Validate", MavButtonVariant.Filled, MavIcons.Check);
        submit.Clicked += (_, _) =>
        {
            var valid = validator.Validate();
            status.Text = valid
                ? "All fields are valid."
                : string.Join(" · ", validator.Errors);
        };

        var card = new MavCard(
            MavCardVariant.Outlined,
            GalleryUi.Column(name, email, city, submit, status))
        {
            Width = 460,
            CardPadding = new Thickness(16),
        };

        return GalleryUi.Showcase(
            "MavFormValidator",
            "Fluent rules: Required, MinLength, MaxLength, Email, Numeric, Pattern, Matches and Must.",
            card,
            "validator.Field(email).Required().Email() · validator.Validate() · validator.Errors");
    }
}
