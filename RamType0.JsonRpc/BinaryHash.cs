using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Numerics;
namespace RamType0.JsonRpc
{
    public static class BinaryHash
    {
        /// <summary>
        /// 中程度の品質のバイナリ列のハッシュコードを高速に生成します。
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static int GetSequenceHashCode(this ReadOnlySpan<byte> span)
        {
            var length = span.Length;
            switch (length)
            {
                case 0:
                    return 0;
                case 1:
                    return Unsafe.ReadUnaligned<byte>(ref MemoryMarshal.GetReference(span)).GetHashCode();
                case 2:
                    return GetElementUnsafeAs<ushort>(span).GetHashCode();
                case 3:
                    return (GetElementUnsafeAs<ushort>(span) | span[2] << 16).GetHashCode();
                case 4:
                    return GetElementUnsafeAs<int>(span).GetHashCode();
                case 5:
                case 6:
                case 7:
                    return (GetElementUnsafeAs<int>(span) ^ GetElementUnsafeAs<int>(span, length - 4)).GetHashCode();
                case 8:
                    return GetElementUnsafeAs<ulong>(span).GetHashCode();
                default:
                    var hash = (uint)((GetElementUnsafeAs<ulong>(span) ^ GetElementUnsafeAs<ulong>(span, length - 8)).GetHashCode());
                    var shifts = length & 31;
                    return (int) BitOperations.RotateRight(hash, shifts);
            }
        }
        /// <summary>
        /// 中程度の品質のバイナリ列のハッシュコードを高速に生成します。
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static int GetSequenceHashCode(this Span<byte> span) => GetSequenceHashCode((ReadOnlySpan<byte>)span);
        private static T GetElementUnsafeAs<T>(ReadOnlySpan<byte> span, int index = 0)
            where T : unmanaged
        {
            return Unsafe.As<byte, T>(ref Unsafe.AsRef(span[index]));
        }
        private static T GetElementUnsafeAs<T>(ReadOnlySpan<byte> span)
            where T : unmanaged
        {
            return Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(span));
        }
    }
}
