using ReQuantum.Contract.Common.Services;
using System.Security.Cryptography;
using System.Text;

namespace ReQuantum.Application.Common.Services;

public sealed class AesEncryptor : IEncryptor
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private static readonly byte[] Key = CreateKey();

    public byte[] Encrypt(byte[] plaintext)
    {
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(Key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, NonceSize + TagSize, ciphertext.Length);
        return payload;
    }

    public byte[]? Decrypt(byte[] ciphertext)
    {
        try
        {
            if (ciphertext.Length < NonceSize + TagSize)
            {
                return null;
            }

            var nonce = new byte[NonceSize];
            var tag = new byte[TagSize];
            var encrypted = new byte[ciphertext.Length - NonceSize - TagSize];

            Buffer.BlockCopy(ciphertext, 0, nonce, 0, NonceSize);
            Buffer.BlockCopy(ciphertext, NonceSize, tag, 0, TagSize);
            Buffer.BlockCopy(ciphertext, NonceSize + TagSize, encrypted, 0, encrypted.Length);

            var plaintext = new byte[encrypted.Length];
            using var aes = new AesGcm(Key, TagSize);
            aes.Decrypt(nonce, encrypted, tag, plaintext);
            return plaintext;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] CreateKey()
    {
        var seed = $"{Environment.UserName}|{Environment.MachineName}|{Environment.OSVersion}|ReQuantum.Encryptor.V1";
        return SHA256.HashData(Encoding.UTF8.GetBytes(seed));
    }
}
