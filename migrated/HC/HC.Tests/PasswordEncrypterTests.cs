using Hc.Services.Cloud;
using Xunit;

namespace Hc.Tests;

/// <summary>
/// Expected values were computed independently from the scheme used by the old app
/// (double MD5, affixes "b"/"P" and "10"/"4", reversal of the hex string).
/// </summary>
public class PasswordEncrypterTests
{
    [Theory]
    [InlineData("", "626afbde4804d4863c7f6b235cc97d26")]
    [InlineData("test", "dfbdde42c0550e367356770033b5f485")]
    [InlineData("12345", "42752f99c47d290fc934578b4fe50e70")]
    [InlineData("Пароль123", "c0528b56b33a9260d5054f8739d09405")]
    public void Encrypt_MatchesServerScheme(string password, string expected)
    {
        Assert.Equal(expected, PasswordEncrypter.Encrypt(password));
    }
}
