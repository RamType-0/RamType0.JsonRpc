using System;
using System.Buffers;

namespace RamType0.JsonRpc.Internal
{
    internal static class ArraySegmentExtension
    {
        public static ArraySegment<byte> CopyToPooled(in this ArraySegment<byte> segment)
        {
            var pooled = ArrayPool<byte>.Shared.Rent(segment.Count);
            Buffer.BlockCopy(segment.Array!, segment.Offset, pooled, 0, segment.Count);
            return new ArraySegment<byte>(pooled, 0, segment.Count);
        }
    }
}
