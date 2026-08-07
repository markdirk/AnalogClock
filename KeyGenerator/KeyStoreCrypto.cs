using System;
using System.Security.Cryptography;
using System.Text;

namespace AnalogClock.KeyGenerator;

public static class KeyStoreCrypto
{
    private static readonly byte[] Salt = new byte[]
    {
        0x5a, 0x1b, 0x9c, 0x3d, 0xef, 0x72, 0x08, 0xa4,
        0xb6, 0x41, 0x2f, 0x88, 0xcd, 0x17, 0x63, 0x0e
    };

    private static readonly byte[] Key = DeriveKey();

    private static byte[] DeriveKey()
    {
        const string password = "KeyGeneratorLicenseListSecret2025";
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
            return null;
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
