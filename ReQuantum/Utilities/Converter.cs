using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ReQuantum.Utilities;

public static class Converter
{
    extension(long number)
    {
        public Guid ToGuid()
        {
            Span<byte> buffer = stackalloc byte[16];
            BinaryPrimitives.WriteInt64LittleEndian(buffer, number);
            return new Guid(buffer.ToArray());
        }
    }

    extension(int number)
    {
        public Guid ToGuid()
        {
            Span<byte> buffer = stackalloc byte[16];
            BinaryPrimitives.WriteInt32LittleEndian(buffer, number);
            return new Guid(buffer.ToArray());
        }
    }

    extension(string str)
    {
        /// <summary>
        /// 将字符串转换为稳定的 Guid（相同字符串总是生成相同 Guid）
        /// </summary>
        public Guid ToGuid()
        {
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(str));
            return new Guid(hash);
        }
    }
}
