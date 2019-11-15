using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Utf8Json;
namespace RamType0.JsonRpc
{
    /// <summary>
    /// エスケープ済みのJson内の文字列を表します。ヌル文字列はサポートされません。
    /// methodの名前の解決などに使います。
    /// </summary>
    [JsonFormatter(typeof(EscapedUTF8String.Formatter))]
    public readonly struct EscapedUTF8String : IEquatable<EscapedUTF8String>
    {
        /// <summary>
        /// Utf8JsonがReadOnlyMemory対応したらReadOnlyMemoryにしたい
        /// </summary>
        readonly ArraySegment<byte> value;
        

        /// <summary>
        /// 引数で与えたUtf8を書き換えてはいけません。また、ダブルクォーテーションで囲われている必要があります。
        /// </summary>
        /// <param name="escapedUtf8"></param>
        private EscapedUTF8String(ArraySegment<byte> escapedUtf8)
        {
            value = escapedUtf8;
        }
        /// <summary>
        /// 引数で与えたUtf8を書き換えてはいけません。また、ダブルクォーテーションで囲われている必要があります。
        /// </summary>
        public static EscapedUTF8String FromEscapedQuoted(ArraySegment<byte> text)
        {
            return new EscapedUTF8String(text);
        }

        public static EscapedUTF8String FromUnEscaped(string text)
        {
            var writer = new JsonWriter();
            writer.WriteString(text);
            var buffer = writer.GetBuffer();
            var array = new byte[buffer.Count];
            Buffer.BlockCopy(buffer.Array!, buffer.Offset, array, 0, array.Length);
            return new EscapedUTF8String(array);
        }

        public int Length => value.Count;
        public override bool Equals(object? obj)
        {
            return obj is EscapedUTF8String @string && Equals(@string);
        }

        public bool Equals([AllowNull] EscapedUTF8String other)
        {
            return value.AsSpan().SequenceEqual(other.value.AsSpan());
        }
        /// <summary>
        /// UnEscapeした文字列を返します。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return new JsonReader(value.Array, value.Offset).ReadString();
        }
        public override int GetHashCode()
        {
            var span = value.AsSpan();
            var length = span.Length;
            switch (length)
            {
                case 0:
                    return 0;

                case 1:
                    return span[0].GetHashCode();
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
                    return (GetElementUnsafeAs<ulong>(span) ^ GetElementUnsafeAs<ulong>(span, length - 8)).GetHashCode();
            }
        }

        private static T GetElementUnsafeAs<T>(ReadOnlySpan<byte> span,int index = 0)
            where T:unmanaged
        {
            return Unsafe.As<byte, T>(ref Unsafe.AsRef(span[index]));
        }

        public static bool operator ==(EscapedUTF8String left, EscapedUTF8String right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EscapedUTF8String left, EscapedUTF8String right)
        {
            return !(left == right);
        }
        public sealed class Formatter : IJsonFormatter<EscapedUTF8String>
        {

            public EscapedUTF8String Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                var quoted = DeserializeUnsafe(ref reader).value;
                var array = new byte[quoted.Count];
                Buffer.BlockCopy(quoted.Array!, quoted.Offset, array, 0, array.Length);

                return FromEscapedQuoted(array);
            }

            /// <summary>
            /// Utf8Jsonの内部バッファをそのまま参照しているため、ヒープに保存したりすると勝手に書き換わります。即座に使用してください。
            /// </summary>
            /// <param name="reader"></param>
            /// <returns></returns>
            public EscapedUTF8String DeserializeUnsafe(ref JsonReader reader)
            {
                var unQuoted = reader.ReadStringSegmentRaw();
                var quoted = new ArraySegment<byte>(unQuoted.Array!, unQuoted.Offset - 1, unQuoted.Count + 2);
                return new EscapedUTF8String(quoted);
            }

            public void Serialize(ref JsonWriter writer, EscapedUTF8String value, IJsonFormatterResolver formatterResolver)
            {
                writer.EnsureCapacity(value.Length);
                foreach (var c in value.value)
                {
                    writer.WriteRawUnsafe(c);
                }
                
            }
        }
        public static implicit operator EscapedUTF8String(string text)
        {
            return FromUnEscaped(text);
        }
    }
}
