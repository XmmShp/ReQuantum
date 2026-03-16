using System.Security.Cryptography;
using System.Text;

namespace ReQuantum.Infrastructure.Utilities;

/// <summary>
/// 通用加密工具，使用 AES-256-GCM，密钥从设备硬件信息派生。
/// </summary>
internal static class Encryption
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private static byte[]? _cachedKey;

    public static byte[] Encrypt(byte[] plaintext)
    {
        var key = GetOrCreateKey();
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSize + TagSize + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSize + TagSize, ciphertext.Length);

        return result;
    }

    public static byte[]? Decrypt(byte[] encrypted)
    {
        if (encrypted.Length < NonceSize + TagSize)
        {
            return null;
        }

        var key = GetOrCreateKey();
        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var ciphertext = new byte[encrypted.Length - NonceSize - TagSize];

        Buffer.BlockCopy(encrypted, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(encrypted, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(encrypted, NonceSize + TagSize, ciphertext, 0, ciphertext.Length);

        var plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
            return plaintext;
        }
        catch
        {
            return null;
        }
    }

    private static byte[] GetOrCreateKey()
    {
        if (_cachedKey is not null)
        {
            return _cachedKey;
        }

        var deviceId = DeviceInfo.Current.Name ?? "ReQuantum";
        var manufacturer = DeviceInfo.Current.Manufacturer ?? "Unknown";
        var model = DeviceInfo.Current.Model ?? "Unknown";
        var platform = DeviceInfo.Current.Platform.ToString();

        var seed = $"{deviceId}|{manufacturer}|{model}|{platform}|ReQuantum.Cookie.Encryption.V1";
        var seedBytes = Encoding.UTF8.GetBytes(seed);

        _cachedKey = SHA256.HashData(seedBytes);
        return _cachedKey;
    }
}
