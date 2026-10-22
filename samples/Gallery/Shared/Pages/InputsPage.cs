using Avalonia.Controls;
using Avalonia;
using Md3.Avalonia;
using Md3.Avalonia.Controls;
using Md3.Avalonia.Theming;

namespace Md3.Gallery;

internal static class InputsPage
{
    public static Control Build()
    {
        return GalleryUi.Page(
            GalleryUi.Section("Text fields", "Outlined and filled fields with icons, password mode, clear button, counters and errors.", TextFields()),
            GalleryUi.Section("Multiline and structured input", "Text areas, PIN/OTP cells, numeric steppers, tags and search.", OtherInputs()),
            GalleryUi.Section("Validation", "MdFormValidator wires rules to fields and writes ErrorText/HasError.", ValidationForm()));
    }

    private static Control TextFields()
    {
        var name = new MdTextField("Name", MdTextFieldVariant.Outlined, "Ada Lovelace", MdIcons.Person);
        var email = new MdTextField("Email", MdTextFieldVariant.Filled, "ada@example.com", MdIcons.Email)
        {
            SupportingText = "We never share it",
        };
        var password = new MdTextField("Password", MdTextFieldVariant.Outlined, "••••••••", MdIcons.Lock)
        {
            IsPassword = true,
            ShowClearButton = true,
        };
        var error = new MdTextField("API key", MdTextFieldVariant.Outlined)
        {
            Text = "abc",
            ErrorText = "The key must be 32 characters",
        };
        var counter = new MdTextField("Code", MdTextFieldVariant.Outlined)
        {
            MaxLength = 12,
            ShowCounter = true,
            Text = "MD3",
        };

        return GalleryUi.Showcase(
            "MdTextField",
            "Single-line field with variants, leading/trailing icons, password reveal, clear button, counter and error state.",
            GalleryUi.Column(name, email, password, error, counter),
            "new MdTextField(label, variant, placeholder, leadingIcon, supportingText) · Text · ErrorText · ContentType · ReturnKeyType");
    }

    private static Control OtherInputs()
    {
        var area = new MdTextArea("Notes", placeholder: "Write a release note…", minHeight: 96)
        {
            Text = "The gallery shows every control.",
            SupportingText = "Markdown is supported by the host app",
        };
        var pin = new MdPinInput { Length = 6, Code = "1234" };
        var number = new MdNumberBox { Label = "Seats", Minimum = 1, Maximum = 20, Value = 4, Suffix = "people" };
        var tags = new MdTagInput("Tags").AddTag("material").AddTag("avalonia");
        var search = new MdSearchBar("Search the gallery");
        search.AttachSearchView(new[] { "MdButton", "MdCard", "MdDataTable", "MdDialog" });

        return GalleryUi.Showcase(
            "MdTextArea, MdPinInput, MdNumberBox, MdTagInput and MdSearchBar",
            "Multiline editor, OTP cells, numeric stepper, tag entry and a search bar with a search view.",
            GalleryUi.Row(
                new MdCard(MdCardVariant.Outlined, area) { Width = 320 },
                new MdCard(MdCardVariant.Outlined, GalleryUi.Column(pin, number)) { Width = 320 },
                new MdCard(MdCardVariant.Outlined, GalleryUi.Column(tags, search)) { Width = 320 }),
            "MdTextArea · MdPinInput · MdNumberBox · MdTagInput · MdSearchBar.AttachSearchView");
    }

    private static Control ValidationForm()
    {
        var name = new MdTextField("Full name", MdTextFieldVariant.Outlined);
        var email = new MdTextField("Email", MdTextFieldVariant.Outlined);
        var city = new MdTextField("City", MdTextFieldVariant.Outlined);
        var status = new MdLabel
        {
            Text = "Submit to validate",
            Role = MdTypeRole.BodySmall,
            Secondary = true,
        };

        var validator = new MdFormValidator { ValidateOnChange = true };
        validator.Field(name).Required().MinLength(2);
        validator.Field(email).Required().Email();
        validator.Field(city).Required();

        var submit = new MdButton("Validate", MdButtonVariant.Filled, MdIcons.Check);
        submit.Clicked += (_, _) =>
        {
            var valid = validator.Validate();
            status.Text = valid
                ? "All fields are valid."
                : string.Join(" · ", validator.Errors);
        };

        var card = new MdCard(
            MdCardVariant.Outlined,
            GalleryUi.Column(name, email, city, submit, status))
        {
            Width = 460,
            CardPadding = new Thickness(16),
        };

        return GalleryUi.Showcase(
            "MdFormValidator",
            "Fluent rules: Required, MinLength, MaxLength, Email, Numeric, Pattern, Matches and Must.",
            card,
            "validator.Field(email).Required().Email() · validator.Validate() · validator.Errors");
    }
}
