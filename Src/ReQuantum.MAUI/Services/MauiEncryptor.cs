using ReQuantum.Application.Common.Services;
using ReQuantum.Contract.Common.Services;
using ReQuantum.Infrastructure.Utilities;

namespace ReQuantum.Infrastructure.Services;

public sealed class MauiEncryptor : IEncryptor
{
    public byte[] Encrypt(byte[] plaintext) => Encryption.Encrypt(plaintext);

    public byte[]? Decrypt(byte[] ciphertext) => Encryption.Decrypt(ciphertext);
}
