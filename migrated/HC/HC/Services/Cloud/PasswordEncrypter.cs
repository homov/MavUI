using System.Security.Cryptography;
using System.Text;

namespace Hc.Services.Cloud;

/// <summary>
/// Password hashing required by the cloud API protocol. The server expects exactly this scheme
/// (double MD5 with fixed affixes and a reversal), so it must not change.
/// </summary>
public static class PasswordEncrypter
{
    /// <summary>Returns the lowercase hex MD5 of the UTF-8 bytes of <paramref name="data"/>.</summary>
    private static string Md5Hex(string data)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>Hashes a password the way the server expects it.</summary>
    public static string Encrypt(string password)
    {
        // Step 1: wrap with the fixed affixes and hash.
        var hash = Md5Hex("b" + password + "P");

        // Step 2: reverse the hex string (the server side does the same).
        var reversed = new string(hash.Reverse().ToArray());

        // Step 3: wrap again and hash once more.
        return Md5Hex("10" + reversed + "4");
    }
}
