using System.Text;

namespace ReQuantum.Contract.Common.Services;

public sealed class Base64Encryptor : IEncryptor
{
    public byte[] Encrypt(byte[] plaintext)
    {
        var base64 = Convert.ToBase64String(plaintext);
        return Encoding.UTF8.GetBytes(base64);
    }

    public byte[]? Decrypt(byte[] ciphertext)
    {
        try
        {
            var base64 = Encoding.UTF8.GetString(ciphertext);
            return Convert.FromBase64String(base64);
        }
        catch
        {
            return null;
        }
    }
}
