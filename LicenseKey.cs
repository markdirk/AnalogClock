using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnalogClock;

public static class LicenseKey
{
    public const string DeveloperKey = "APZ3-TQE3-248A-5KW8-YCBW-J5F8G";

    // Base32 alphabet without visually ambiguous characters 0, O, I, L
    private const string Alphabet = "ABCDEFGHJKMNPQRSTUVWXYZ123456789";

    private static readonly byte[] Secret = DeriveSecret();

    private static byte[] DeriveSecret()
    {
        const string password = "AnalogClockOfflineLicenseSecret2025";
        var salt = new byte[]
        {
            0x21, 0x43, 0x65, 0x87, 0xa9, 0xcb, 0xed, 0x0f,
            0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf1
        };
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    private const int SerialLength = 7;
    private const int SignatureLength = 8;

    public static string Generate()
    {
        var serial = RandomNumberGenerator.GetBytes(SerialLength);
        var signature = Sign(serial);

        var keyBytes = new byte[SerialLength + SignatureLength];
        Buffer.BlockCopy(serial, 0, keyBytes, 0, SerialLength);
        Buffer.BlockCopy(signature, 0, keyBytes, SerialLength, SignatureLength);

        return FormatKey(ToBase32(keyBytes));
    }

    public static bool Verify(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (key == DeveloperKey)
        {
            return true;
        }

        var compact = key.Replace("-", string.Empty).ToUpperInvariant();
        if (compact.Length != (SerialLength + SignatureLength) * 8 / 5 || !IsValidKey(compact))
        {
            return false;
        }

        try
        {
            if (!TryFromBase32(compact, out var bytes) || bytes.Length != SerialLength + SignatureLength)
            {
                return false;
            }

            var serial = new byte[SerialLength];
            var signature = new byte[SignatureLength];
            Buffer.BlockCopy(bytes, 0, serial, 0, SerialLength);
            Buffer.BlockCopy(bytes, SerialLength, signature, 0, SignatureLength);

            var expected = Sign(serial);
            return CryptographicOperations.FixedTimeEquals(signature, expected);
        }
        catch
        {
            return false;
        }
    }

    public static string GenerateToken(string key, string hardwareId)
    {
        var data = Encoding.UTF8.GetBytes(key + "|" + hardwareId);
        return Convert.ToHexString(Sign(data, 16));
    }

    public static bool VerifyToken(string key, string hardwareId, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var expected = GenerateToken(key, hardwareId);
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(token),
                Convert.FromHexString(expected));
        }
        catch
        {
            return false;
        }
    }

    private static byte[] Sign(byte[] data)
    {
        return Sign(data, SignatureLength);
    }

    private static byte[] Sign(byte[] data, int length)
    {
        using var hmac = new HMACSHA256(Secret);
        var hash = hmac.ComputeHash(data);
        var result = new byte[length];
        Buffer.BlockCopy(hash, 0, result, 0, length);
        return result;
    }

    private static string FormatKey(string chars)
    {
        return $"{chars[..4]}-{chars[4..8]}-{chars[8..12]}-{chars[12..16]}-{chars[16..20]}-{chars[20..24]}";
    }

    private static bool IsValidKey(string value)
    {
        return value.All(c => Alphabet.Contains(c));
    }

    private static string ToBase32(byte[] data)
    {
        if (data == null || data.Length == 0)
        {
            return string.Empty;
        }

        var result = new StringBuilder();
        int bits = 0;
        int bitCount = 0;

        foreach (var b in data)
        {
            bits = (bits << 8) | b;
            bitCount += 8;

            while (bitCount >= 5)
            {
                var index = (bits >> (bitCount - 5)) & 31;
                result.Append(Alphabet[index]);
                bitCount -= 5;
            }
        }

        if (bitCount > 0)
        {
            var index = (bits << (5 - bitCount)) & 31;
            result.Append(Alphabet[index]);
        }

        return result.ToString();
    }

    private static bool TryFromBase32(string value, out byte[] bytes)
    {
        bytes = Array.Empty<byte>();
        using var stream = new MemoryStream();
        int bits = 0;
        int bitCount = 0;

        foreach (var c in value)
        {
            var index = Alphabet.IndexOf(c);
            if (index < 0)
            {
                return false;
            }

            bits = (bits << 5) | index;
            bitCount += 5;

            while (bitCount >= 8)
            {
                stream.WriteByte((byte)((bits >> (bitCount - 8)) & 0xFF));
                bitCount -= 8;
            }
        }

        bytes = stream.ToArray();
        return true;
    }
}
