using System.Security.Cryptography;

namespace Bonzer.Domain.Common;

/// <summary>
/// RFC 9562 UUID version 7. Time ordered, so primary-key B-tree indexes stay compact
/// (random v4 UUIDs fragment indexes on large tables such as attendance and marks).
/// </summary>
public static class UuidV7
{
    public static Guid NewGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[0] = (byte)(ms >> 40);
        bytes[1] = (byte)(ms >> 32);
        bytes[2] = (byte)(ms >> 24);
        bytes[3] = (byte)(ms >> 16);
        bytes[4] = (byte)(ms >> 8);
        bytes[5] = (byte)ms;
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70); // version 7
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80); // RFC variant

        return new Guid(bytes, bigEndian: true);
    }
}
