using System.Security.Cryptography;
using System.Text;

namespace EstateFlow.Data.Security;

// Encrypts sensitive columns (ID numbers, bank/policy references) before they
// reach Azure SQL, so a leaked database backup or connection string does not
// expose them in plain text (POPIA).
public interface IFieldProtector
{
    // Identifies the key, so EF Core builds a separate model per key.
    string KeyId { get; }

    string Protect(string plaintext);

    // Values written before encryption was switched on have no prefix and
    // are returned unchanged; they are encrypted the next time they're saved.
    string Unprotect(string stored);
}

// AES-256-GCM. Stored format: "enc:v1:" + base64(nonce | ciphertext | tag).
public sealed class AesGcmFieldProtector : IFieldProtector
{
    public const string Prefix = "enc:v1:";

    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmFieldProtector(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("The field encryption key must be exactly 32 bytes (256 bits).", nameof(key));

        _key = key;
        KeyId = Convert.ToHexString(SHA256.HashData(key))[..12];
    }

    public string KeyId { get; }

    // Reads "DataProtection:FieldKey" (base64). Returns null when not configured.
    public static AesGcmFieldProtector? FromBase64Key(string? base64Key)
    {
        if (string.IsNullOrWhiteSpace(base64Key))
            return null;

        byte[] key;
        try
        {
            key = Convert.FromBase64String(base64Key.Trim());
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                "DataProtection:FieldKey must be a base64-encoded 32-byte key.", ex);
        }

        return new AesGcmFieldProtector(key);
    }

    public string Protect(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext) || plaintext.StartsWith(Prefix, StringComparison.Ordinal))
            return plaintext;

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var buffer = new byte[NonceSize + plainBytes.Length + TagSize];

        var nonce = buffer.AsSpan(0, NonceSize);
        var cipher = buffer.AsSpan(NonceSize, plainBytes.Length);
        var tag = buffer.AsSpan(NonceSize + plainBytes.Length, TagSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        return Prefix + Convert.ToBase64String(buffer);
    }

    public string Unprotect(string stored)
    {
        if (string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix, StringComparison.Ordinal))
            return stored;

        var buffer = Convert.FromBase64String(stored[Prefix.Length..]);
        var cipherLength = buffer.Length - NonceSize - TagSize;

        var plainBytes = new byte[cipherLength];

        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(
            buffer.AsSpan(0, NonceSize),
            buffer.AsSpan(NonceSize, cipherLength),
            buffer.AsSpan(NonceSize + cipherLength, TagSize),
            plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
