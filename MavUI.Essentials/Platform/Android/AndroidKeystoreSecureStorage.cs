using System.Security.Cryptography;
using System.Text;
using Android.Content;
using Android.Security;
using Java.Math;
using Java.Security;
using Java.Security.Spec;
using Javax.Crypto;
using Javax.Security.Auth.X500;
using JKeyStore = Java.Security.KeyStore;

namespace Mav.Essentials.Droid;

/// <summary>
/// SecureStorage for Android 5.0+ (API 21).
/// </summary>
/// <remarks>
/// API 21 cannot create AES keys inside the Android Keystore (that needs API 23). Instead:
/// <list type="number">
/// <item>An RSA-2048 key pair is created in the Keystore. Its private key never leaves the Keystore.</item>
/// <item>64 random bytes are generated once: a 256-bit AES key and a 256-bit HMAC key. They are stored
/// RSA-encrypted (PKCS#1 v1.5) in a private SharedPreferences file.</item>
/// <item>Each value is AES-256-CBC encrypted with a random IV and authenticated with HMAC-SHA256 (encrypt-then-MAC).</item>
/// </list>
/// The unwrapped AES and HMAC keys are held in process memory. This is weaker than API 23+ Keystore AES keys,
/// where the key material never reaches the app. Writes and reads are synchronous; the methods return completed tasks.
/// </remarks>
public sealed class AndroidKeystoreSecureStorage : IMavSecureStorage
{
    private const string WrapAlias = "mav.essentials.wrap.v1";
    private const string PrefsFile = "mav.essentials.secure";
    private const string WrappedKeyEntry = "__mav_wrapped_key_v1";
    private const string ValuePrefix = "v:";
    private const int AesKeyBytes = 32;
    private const int HmacKeyBytes = 32;
    private const int IvBytes = 16;
    private const int MacBytes = 32;

    private readonly Context _context;
    private readonly object _gate = new();
    private byte[]? _encKey;
    private byte[]? _macKey;

    public AndroidKeystoreSecureStorage(Context context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<string?> GetAsync(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_gate)
        {
            var stored = Prefs.GetString(ValuePrefix + key, null);
            if (stored is null)
            {
                return Task.FromResult<string?>(null);
            }

            var (enc, mac) = Keys();
            return Task.FromResult<string?>(Decrypt(Convert.FromBase64String(stored), enc, mac));
        }
    }

    public Task SetAsync(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        lock (_gate)
        {
            var (enc, mac) = Keys();
            var blob = Encrypt(value, enc, mac);
            var editor = Prefs.Edit() ?? throw new InvalidOperationException("SharedPreferences editor is unavailable.");
            editor.PutString(ValuePrefix + key, Convert.ToBase64String(blob));
            editor.Apply();
        }

        return Task.CompletedTask;
    }

    public bool Remove(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);

        lock (_gate)
        {
            var name = ValuePrefix + key;
            var existed = Prefs.Contains(name);
            var editor = Prefs.Edit() ?? throw new InvalidOperationException("SharedPreferences editor is unavailable.");
            editor.Remove(name);
            editor.Apply();
            return existed;
        }
    }

    /// <summary>Deletes every stored secret and the wrapped data key. The Keystore key pair is kept.</summary>
    public void RemoveAll()
    {
        lock (_gate)
        {
            var editor = Prefs.Edit() ?? throw new InvalidOperationException("SharedPreferences editor is unavailable.");
            editor.Clear();
            editor.Apply();
            _encKey = null;
            _macKey = null;
        }
    }

    private global::Android.Content.ISharedPreferences Prefs =>
        _context.GetSharedPreferences(PrefsFile, FileCreationMode.Private)
        ?? throw new InvalidOperationException("SharedPreferences are unavailable.");

    private (byte[] Enc, byte[] Mac) Keys()
    {
        if (_encKey is null || _macKey is null)
        {
            var material = LoadOrCreateKeyMaterial();
            _encKey = material[..AesKeyBytes];
            _macKey = material[AesKeyBytes..];
        }

        return (_encKey, _macKey);
    }

    private byte[] LoadOrCreateKeyMaterial()
    {
        var keyStore = JKeyStore.GetInstance("AndroidKeyStore")
            ?? throw new InvalidOperationException("Android Keystore is unavailable.");
        keyStore.Load(null, null);
        EnsureWrappingKeyPair(keyStore);

        var wrapped = Prefs.GetString(WrappedKeyEntry, null);
        if (wrapped is not null)
        {
            var privateKey = keyStore.GetKey(WrapAlias, null) as IPrivateKey
                ?? throw new InvalidOperationException("Keystore private key is missing.");
            return Unwrap(Convert.FromBase64String(wrapped), privateKey);
        }

        var material = RandomNumberGenerator.GetBytes(AesKeyBytes + HmacKeyBytes);
        var certificate = keyStore.GetCertificate(WrapAlias)
            ?? throw new InvalidOperationException("Keystore certificate is missing.");
        var blob = Wrap(material, certificate.PublicKey);

        var editor = Prefs.Edit() ?? throw new InvalidOperationException("SharedPreferences editor is unavailable.");
        editor.PutString(WrappedKeyEntry, Convert.ToBase64String(blob));
        editor.Apply();
        return material;
    }

    private void EnsureWrappingKeyPair(JKeyStore keyStore)
    {
        if (keyStore.ContainsAlias(WrapAlias))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var spec = new KeyPairGeneratorSpec.Builder(_context)
            .SetAlias(WrapAlias)
            .SetSubject(new X500Principal("CN=MavUI.Essentials"))
            .SetSerialNumber(BigInteger.One)
            .SetStartDate(new global::Java.Util.Date(now.ToUnixTimeMilliseconds()))
            .SetEndDate(new global::Java.Util.Date(now.AddYears(25).ToUnixTimeMilliseconds()))
            .SetAlgorithmParameterSpec(new RSAKeyGenParameterSpec(2048, RSAKeyGenParameterSpec.F4))
            .Build();

        var generator = KeyPairGenerator.GetInstance("RSA", "AndroidKeyStore")
            ?? throw new InvalidOperationException("RSA key generation in the Keystore is unavailable.");
        generator.Initialize(spec);
        generator.GenerateKeyPair();
    }

    private static byte[] Wrap(byte[] material, IPublicKey publicKey)
    {
        var cipher = Cipher.GetInstance("RSA/ECB/PKCS1Padding", "AndroidOpenSSL")
            ?? throw new InvalidOperationException("RSA cipher is unavailable.");
        cipher.Init(Cipher.EncryptMode, publicKey);
        return cipher.DoFinal(material) ?? throw new InvalidOperationException("Key wrapping returned no data.");
    }

    private static byte[] Unwrap(byte[] blob, IPrivateKey privateKey)
    {
        var cipher = Cipher.GetInstance("RSA/ECB/PKCS1Padding", "AndroidOpenSSL")
            ?? throw new InvalidOperationException("RSA cipher is unavailable.");
        cipher.Init(Cipher.DecryptMode, privateKey);
        var material = cipher.DoFinal(blob) ?? throw new InvalidOperationException("Key unwrapping returned no data.");
        if (material.Length != AesKeyBytes + HmacKeyBytes)
        {
            throw new CryptographicException("Wrapped key material has an unexpected length.");
        }

        return material;
    }

    private static byte[] Encrypt(string plaintext, byte[] enc, byte[] mac)
    {
        var iv = RandomNumberGenerator.GetBytes(IvBytes);
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor(enc, iv);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

        var body = new byte[IvBytes + cipher.Length];
        Buffer.BlockCopy(iv, 0, body, 0, IvBytes);
        Buffer.BlockCopy(cipher, 0, body, IvBytes, cipher.Length);

        var tag = HMACSHA256.HashData(mac, body);
        var result = new byte[body.Length + MacBytes];
        Buffer.BlockCopy(body, 0, result, 0, body.Length);
        Buffer.BlockCopy(tag, 0, result, body.Length, MacBytes);
        return result;
    }

    private static string Decrypt(byte[] blob, byte[] enc, byte[] mac)
    {
        if (blob.Length < IvBytes + 16 + MacBytes)
        {
            throw new CryptographicException("Stored secret is malformed.");
        }

        var body = blob[..^MacBytes];
        var tag = blob[^MacBytes..];
        var expected = HMACSHA256.HashData(mac, body);
        if (!CryptographicOperations.FixedTimeEquals(expected, tag))
        {
            throw new CryptographicException("Stored secret failed its integrity check.");
        }

        var iv = body[..IvBytes];
        var cipher = body[IvBytes..];
        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor(enc, iv);
        var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
        return Encoding.UTF8.GetString(plain);
    }
}
