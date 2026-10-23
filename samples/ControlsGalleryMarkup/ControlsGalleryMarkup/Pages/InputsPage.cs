using System;
using Avalonia.Controls;
using Avalonia.Media;
using Mav;
using Mav.Controls;
using Mav.Theming;
using static Mav.ControlsGalleryMarkup.Pages.PageParts;

namespace Mav.ControlsGalleryMarkup.Pages;

/// <summary>Text fields, multi-line and structured input, and form validation. Mirrors the AXAML Inputs page.</summary>
public static class InputsPage
{
    public static Control Build()
    {
        var name = new MavTextField(L("Inputs.Field.Name", "Name"), MavTextFieldVariant.Outlined)
            .LeadingIcon(MavIcons.Person)
            .Text("Ada Lovelace");
        var email = new MavTextField(L("Inputs.Field.Email", "Email"), MavTextFieldVariant.Filled)
            .LeadingIcon(MavIcons.Email)
            .SupportingText(L("Inputs.Field.Email.Support", "We never share it"))
            .Text("ada@example.com");
        var password = new MavTextField(L("Inputs.Field.Password", "Password"), MavTextFieldVariant.Outlined)
            .LeadingIcon(MavIcons.Lock)
            .Text("password")
            .IsPassword(true)
            .ShowClearButton(true);
        var apiKey = new MavTextField(L("Inputs.Field.ApiKey", "API key"), MavTextFieldVariant.Outlined)
            .Text("abc")
            .ErrorText(L("Inputs.Field.ApiKey.Error", "The key must be 32 characters"));
        var code = new MavTextField(L("Inputs.Field.Code", "Code"), MavTextFieldVariant.Outlined)
            .Text("MD3")
            .MaxLength(12)
            .ShowCounter(true);

        var notes = new MavTextArea(L("Inputs.Area.Label", "Notes"), MavTextFieldVariant.Outlined)
            .Placeholder(L("Inputs.Area.Placeholder", "Write a release note…"))
            .SupportingText(L("Inputs.Area.Support", "Markdown is supported by the host app"))
            .Text("The gallery shows every control.")
            .MinBoxHeight(96);

        var pin = new MavPinInput(6).Code("1234");
        var seats = new MavNumberBox()
            .Label(L("Inputs.Number.Label", "Seats"))
            .Minimum(1)
            .Maximum(20)
            .Value(4)
            .Suffix(L("Inputs.Number.Suffix", "people"));

        var tags = new MavTagInput(L("Inputs.Tags.Label", "Tags"))
            .AddTag("material")
            .AddTag("avalonia");
        var search = new MavSearchBar(L("Inputs.Search.Placeholder", "Search the gallery"))
            .AttachSuggestions("MavButton", "MavCard", "MavDataTable", "MavDialog");

        var nameField = new MavTextField(L("Inputs.Form.Name", "Full name"), MavTextFieldVariant.Outlined);
        var emailField = new MavTextField(L("Inputs.Field.Email", "Email"), MavTextFieldVariant.Outlined);
        var cityField = new MavTextField(L("Inputs.Form.City", "City"), MavTextFieldVariant.Outlined);

        // MavFormValidator is not a control, so it has no generated fluent methods. It validates on change by default.
        var validator = new MavFormValidator()
            .Rule(nameField, r => r.Required().MinLength(2))
            .Rule(emailField, r => r.Required().Email())
            .Rule(cityField, r => r.Required());

        var status = new MavLabel(L("Inputs.Form.Status", "Submit to validate"), MavTypeRole.BodySmall)
            .Secondary(true)
            .TextWrapping(TextWrapping.Wrap);
        var submit = new MavButton(L("Inputs.Form.Validate", "Validate"), MavButtonVariant.Filled, MavIcons.Check)
            .OnClicked(() =>
            {
                var valid = validator.Validate();
                status.Text = valid
                    ? MavLocalization.Get("Inputs.Form.Valid", "All fields are valid.")
                    : string.Join(" · ", validator.Errors);
            });

        return Page(
            Section(
                "Inputs.Fields.Section", "Text fields",
                "Inputs.Fields.Section.Body", "Outlined and filled fields with icons, password mode, clear button, counters and errors.",
                Card(
                    "MavTextField",
                    "Inputs.Fields.Card.Body", "Single-line field with variants, leading/trailing icons, password reveal, clear button, counter and error state.",
                    "new MavTextField(label, variant, placeholder, leadingIcon, supportingText) · Text · ErrorText · ContentType · ReturnKeyType",
                    name, email, password, apiKey, code)),
            Section(
                "Inputs.Other.Section", "Multi-line and structured input",
                "Inputs.Other.Section.Body", "Text areas, PIN/OTP cells, numeric steppers, tags and search.",
                Card(
                    "MavTextArea, MavPinInput, MavNumberBox, MavTagInput and MavSearchBar",
                    "Inputs.Other.Card.Body", "Multi-line editor, OTP cells, numeric stepper, tag entry and a search bar with a search view.",
                    "MavTextArea · MavPinInput · MavNumberBox · MavTagInput · MavSearchBar.AttachSearchView",
                    Adaptive(300,
                        Plain(notes),
                        Plain(new StackPanel().Spacing(8).Children(pin, seats)),
                        Plain(new StackPanel().Spacing(8).Children(tags, search))))),
            Section(
                "Inputs.Validation.Section", "Validation",
                "Inputs.Validation.Section.Body", "MavFormValidator wires rules to fields and writes ErrorText/HasError.",
                Card(
                    "MavFormValidator",
                    "Inputs.Validation.Card.Body", "Fluent rules: Required, MinLength, MaxLength, Email, Numeric, Pattern, Matches and Must.",
                    "validator.Field(email).Required().Email() · validator.Validate() · validator.Errors",
                    Plain(new StackPanel().Spacing(10).Children(nameField, emailField, cityField, submit, status)))));
    }

    /// <summary>Adds validation rules for one field and returns the validator for chaining.</summary>
    private static MavFormValidator Rule(this MavFormValidator validator, MavTextField field, Func<MavFieldRuleBuilder, MavFieldRuleBuilder> rules)
    {
        rules(validator.Field(field));
        return validator;
    }

    /// <summary>Shows search suggestions and returns the search bar for chaining.</summary>
    private static MavSearchBar AttachSuggestions(this MavSearchBar bar, params string[] suggestions)
    {
        bar.AttachSearchView(suggestions);
        return bar;
    }
}
