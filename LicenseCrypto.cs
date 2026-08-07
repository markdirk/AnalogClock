using System;
using System.Security.Cryptography;
using System.Text;

namespace AnalogClock;

public static class LicenseCrypto
{
    private static readonly byte[] Salt = new byte[]
    {
        0x12, 0x34, 0x56, 0x78, 0x9a, 0xbc, 0xde, 0xf0,
        0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88
    };

    private static readonly byte[] Key = DeriveKey();

    private static byte[] DeriveKey()
    {
        const string password = "AnalogClock2025LicenseSecret";
        return Rfc2898DeriveBytes.Pbkdf2(password, Salt, 100_000, HashAlgorithmName.SHA256, 32);
    }

    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return string.Empty;
        }

        try
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plain = Encoding.UTF8.GetBytes(plainText);
            var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);

            var result = new byte[aes.IV.Length + cipher.Length];
            Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
            Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);

            return Convert.ToBase64String(result);
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string? Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
        {
            return string.Empty;
        }

        try
        {
            var data = Convert.FromBase64String(cipherText);
            if (data.Length < 16)
            {
                return null;
            }

            var iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            var cipher = new byte[data.Length - 16];
            Buffer.BlockCopy(data, 16, cipher, 0, cipher.Length);

            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var plain = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }
}
