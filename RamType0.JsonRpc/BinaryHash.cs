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
            ref var spanRef = ref MemoryMarshal.GetReference(span);
            switch (length)
            {
                case 0:
                    return 0;
                case 1:
                    return Unsafe.ReadUnaligned<byte>(ref spanRef).GetHashCode();
                case 2:
                    return GetElementUnsafeAs<ushort>(ref spanRef).GetHashCode();
                case 3:
                    return (GetElementUnsafeAs<ushort>(ref spanRef) | Unsafe.AddByteOffset(ref spanRef,(IntPtr)2) << 16) .GetHashCode();
                case 4:
                    return GetElementUnsafeAs<int>(ref spanRef).GetHashCode();
                case 5:
                case 6:
                case 7:
                    return (GetElementUnsafeAs<int>(ref spanRef) ^ GetElementUnsafeAs<int>(ref spanRef, length - 4)).GetHashCode();
                case 8:
                    return GetElementUnsafeAs<ulong>(ref spanRef).GetHashCode();
                default:
                    var hash = (uint)((GetElementUnsafeAs<ulong>(ref spanRef) ^ GetElementUnsafeAs<ulong>(ref spanRef, length - 8)).GetHashCode());
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
        private static T GetElementUnsafeAs<T>(ref byte spanRef, int index)
            where T : unmanaged
        {
            return Unsafe.ReadUnaligned<T>(ref Unsafe.AddByteOffset(ref spanRef,(IntPtr)index));
        }
        private static T GetElementUnsafeAs<T>(ref byte spanRef)
            where T : unmanaged
        {
            return Unsafe.ReadUnaligned<T>(ref spanRef);
        }
    }
}
