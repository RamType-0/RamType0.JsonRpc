using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;
namespace RamType0.JsonRpc
{
    internal static class ObjectReceiveHelper
    {
        public static bool ReadIsValidJsonRpcMember(ref this JsonReader reader)
        {
            return reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'j', (byte)'s', (byte)'o', (byte)'n', (byte)'r', (byte)'p', (byte)'c', }) &
            reader.ReadStringSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0' });
        }

        public static void ReadIsValidJsonRpcMemberWithVerify(ref this JsonReader reader)
        {
            if (!reader.ReadIsValidJsonRpcMember())
            {
                ThrowJsonRpcFormatException();
                return;
            }
        }

        private static void ThrowJsonRpcFormatException()
        {
            throw new FormatException("JSON-RPCオブジェクトのjsonrpcメンバーが不正です。");
        }
    }
}
