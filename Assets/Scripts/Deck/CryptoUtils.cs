using System;
using System.Security.Cryptography;
using System.Text;

public static class CryptoUtil
{
    private static readonly byte[] iv = new byte[16]; // zero IV (must match for encrypt/decrypt)

    public static string Encrypt(string plainText, string key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor();
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);

            byte[] encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Convert.ToBase64String(encrypted);
        }
    }

    public static string Decrypt(string cipherText, string key)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor();
            byte[] bytes = Convert.FromBase64String(cipherText);

            byte[] decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
