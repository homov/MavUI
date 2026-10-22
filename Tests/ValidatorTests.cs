using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Mav.Controls;
using Xunit;

namespace Mav.Tests;

public class ValidatorTests
{
    [AvaloniaFact]
    public void Required_flags_empty_field_and_sets_error_text()
    {
        var name = new MavTextField("Name");
        var validator = new MavFormValidator();
        validator.Field(name).Required();

        Assert.False(validator.Validate());
        Assert.False(validator.IsValid);
        Assert.Single(validator.Errors);
        Assert.Equal("This field is required", name.ErrorText);
        Assert.True(name.HasError);
    }

    [AvaloniaFact]
    public void Required_passes_when_field_has_text()
    {
        var name = new MavTextField("Name") { Text = "Ada" };
        var validator = new MavFormValidator();
        validator.Field(name).Required();

        Assert.True(validator.Validate());
        Assert.True(validator.IsValid);
        Assert.Null(name.ErrorText);
    }

    [AvaloniaFact]
    public void Email_rejects_invalid_value()
    {
        var email = new MavTextField("Email") { Text = "not-an-email" };
        var validator = new MavFormValidator();
        validator.Field(email).Required().Email();

        Assert.False(validator.Validate());
        Assert.Equal("Enter a valid email address", email.ErrorText);

        email.Text = "ada@example.com";
        Assert.True(validator.Validate());
    }

    [AvaloniaFact]
    public void MinLength_and_MaxLength()
    {
        var field = new MavTextField("Code") { Text = "a" };
        var validator = new MavFormValidator();
        validator.Field(field).MinLength(3).MaxLength(5);

        Assert.False(validator.Validate());
        Assert.Contains("at least 3", field.ErrorText);

        field.Text = "abcd";
        Assert.True(validator.Validate());

        field.Text = "abcdef";
        Assert.False(validator.Validate());
        Assert.Contains("at most 5", field.ErrorText);
    }

    [AvaloniaFact]
    public void Numeric_rule()
    {
        var field = new MavTextField("Age") { Text = "abc" };
        var validator = new MavFormValidator();
        validator.Field(field).Numeric();

        Assert.False(validator.Validate());
        field.Text = "42";
        Assert.True(validator.Validate());
    }

    [AvaloniaFact]
    public void Pattern_rule()
    {
        var field = new MavTextField("Code") { Text = "abc" };
        var validator = new MavFormValidator();
        validator.Field(field).Pattern(@"^[A-Z]{3}$");

        Assert.False(validator.Validate());
        field.Text = "ABC";
        Assert.True(validator.Validate());
    }

    [AvaloniaFact]
    public void Matches_rule()
    {
        var password = new MavTextField("Password") { Text = "secret" };
        var confirm = new MavTextField("Confirm") { Text = "different" };
        var validator = new MavFormValidator();
        validator.Field(password).Required();
        validator.Field(confirm).Required().Matches(() => password.Text);

        Assert.False(validator.Validate());
        confirm.Text = "secret";
        Assert.True(validator.Validate());
    }

    [AvaloniaFact]
    public void Must_rule_uses_custom_predicate()
    {
        var field = new MavTextField("Even") { Text = "3" };
        var validator = new MavFormValidator();
        validator.Field(field).Must(v => int.TryParse(v, out var n) && n % 2 == 0, "Must be even");

        Assert.False(validator.Validate());
        Assert.Equal("Must be even", field.ErrorText);
        field.Text = "4";
        Assert.True(validator.Validate());
    }

    [AvaloniaFact]
    public void ClearErrors_resets_fields()
    {
        var field = new MavTextField("Name");
        var validator = new MavFormValidator();
        validator.Field(field).Required();
        validator.Validate();

        validator.ClearErrors();

        Assert.True(validator.IsValid);
        Assert.Null(field.ErrorText);
    }

    [AvaloniaFact]
    public void Attached_validation_properties_validate_tree()
    {
        var field = new MavTextField("Name");
        MavValidation.SetRequired(field, true);
        MavValidation.SetEmail(field, true);

        var host = new StackPanel { Children = { field } };
        var window = new Window { Content = host };
        window.Show();

        Assert.False(MavValidation.ValidateTree(host));
        Assert.Equal("This field is required", field.ErrorText);

        field.Text = "ada@example.com";
        Assert.True(MavValidation.ValidateTree(host));

        window.Close();
    }
}
