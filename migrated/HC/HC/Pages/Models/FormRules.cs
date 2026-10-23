namespace Hc.Pages.Models;

/// <summary>Input checks shared by the account forms. They only check the shape of the input, not the server rules.</summary>
public static class FormRules
{
    /// <summary>True for "name@domain.tld" with no spaces: an @ with text before it, and a dot after it with text on both sides.</summary>
    public static bool IsValidEmail(string email)
    {
        if (email.Length == 0 || email.Any(char.IsWhiteSpace))
            return false;

        var at = email.IndexOf('@');
        if (at <= 0 || at != email.LastIndexOf('@'))
            return false;

        var dot = email.LastIndexOf('.');
        return dot > at + 1 && dot < email.Length - 1;
    }

    /// <summary>The short code from the email must be exactly four digits.</summary>
    public static bool IsValidCode(string code) => code.Length == 4 && code.All(char.IsAsciiDigit);
}
