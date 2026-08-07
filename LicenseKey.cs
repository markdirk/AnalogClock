using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace AnalogClock;

public static class LicenseKey
{
    public const string DeveloperKey = "APZ3-TQE3-248A-5KW8-YCBW-J5F8G";

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

    private const int SerialLength = 6;
    private const int SignatureLength = 6;

    public static string Generate()
    {
        var serial = RandomNumberGenerator.GetBytes(SerialLength);
        var signature = Sign(serial);

        var keyBytes = new byte[SerialLength + SignatureLength];
        Buffer.BlockCopy(serial, 0, keyBytes, 0, SerialLength);
        Buffer.BlockCopy(signature, 0, keyBytes, SerialLength, SignatureLength);

        return FormatKey(Convert.ToHexString(keyBytes));
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
        if (compact.Length != (SerialLength + SignatureLength) * 2 || !IsHex(compact))
        {
            return false;
        }

        try
        {
            var bytes = Convert.FromHexString(compact);
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

    private static string FormatKey(string hex)
    {
        return $"{hex[..4]}-{hex[4..8]}-{hex[8..12]}-{hex[12..16]}-{hex[16..20]}-{hex[20..24]}";
    }

    private static bool IsHex(string value)
    {
        return value.All(c => char.IsDigit(c) || (c >= 'A' && c <= 'F'));
    }
}
