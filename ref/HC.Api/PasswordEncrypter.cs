namespace HC.Api;

public static class PasswordEncrypter
{
    private static string CustomMd5(string data)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(data));
        var builder = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            builder.Append(b.ToString("x2"));
        return builder.ToString();
    }

    public static string Encrypt(string password)
    {
        password = "b" + password + "P";
        password = CustomMd5(password);
        var array = password.ToCharArray();
        Array.Reverse(array);
        password = new string(array);
        password = "10" + password + "4";
        password = CustomMd5(password);
        return password;
    }
}
