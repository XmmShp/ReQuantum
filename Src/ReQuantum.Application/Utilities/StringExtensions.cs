using System.Security.Cryptography;
using System.Text;

namespace ReQuantum.Application.Utilities;

public static class StringExtensions
{
    public static Guid ToGuid(this string input)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}
