using System.Text;

namespace ReQuantum.Application.Common.Services;

public interface IEncryptor
{
    byte[] Encrypt(byte[] plaintext);

    byte[]? Decrypt(byte[] ciphertext);

    string EncryptToBase64(string plaintext)
        => Convert.ToBase64String(Encrypt(Encoding.UTF8.GetBytes(plaintext)));

    string? TryDecryptFromBase64(string cipherTextBase64)
    {
        try
        {
            var payload = Convert.FromBase64String(cipherTextBase64);
            var plaintext = Decrypt(payload);
            return plaintext is null ? null : Encoding.UTF8.GetString(plaintext);
        }
        catch
        {
            return null;
        }
    }
}
