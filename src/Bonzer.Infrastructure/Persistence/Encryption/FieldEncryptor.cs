using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;

namespace Bonzer.Infrastructure.Persistence.Encryption;

public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>Base64 encoded 32-byte AES key. Keep in a secret store / environment, never in source control.</summary>
    [Required] public string Key { get; init; } = string.Empty;
}

public interface IFieldEncryptor
{
    string Encrypt(string plaintext);
    string Decrypt(string stored);
}

/// <summary>
/// AES-256-GCM column encryption for [Encrypted] properties (Aadhaar / national IDs).
/// Stored format: "v1:" + base64(nonce | tag | ciphertext). Values without the prefix are returned
/// unchanged so existing plaintext data can be migrated gradually.
/// </summary>
public sealed class AesGcmFieldEncryptor : IFieldEncryptor
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private readonly byte[] _key;

    public AesGcmFieldEncryptor(IOptions<EncryptionOptions> options) : this(options.Value.Key) { }

    public AesGcmFieldEncryptor(string base64Key)
    {
        _key = Convert.FromBase64String(base64Key);
        if (_key.Length != 32)
            throw new InvalidOperationException("Encryption:Key must be a base64 encoded 32-byte key.");
    }

    public string Encrypt(string plaintext)
    {
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var output = new byte[NonceSize + TagSize + plain.Length];
        var nonce = output.AsSpan(0, NonceSize);
        var tag = output.AsSpan(NonceSize, TagSize);
        var cipher = output.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);
        using var aes = new AesGcm(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);
        return Prefix + Convert.ToBase64String(output);
    }

    public string Decrypt(string stored)
    {
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;

        var data = Convert.FromBase64String(stored[Prefix.Length..]);
        var plain = new byte[data.Length - NonceSize - TagSize];
        using var aes = new AesGcm(_key, TagSize);
        aes.Decrypt(data.AsSpan(0, NonceSize), data.AsSpan(NonceSize + TagSize), data.AsSpan(NonceSize, TagSize), plain);
        return Encoding.UTF8.GetString(plain);
    }
}

public sealed class EncryptedStringConverter(IFieldEncryptor encryptor)
    : ValueConverter<string, string>(v => encryptor.Encrypt(v), v => encryptor.Decrypt(v));
