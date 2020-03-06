using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Utf8Json;
namespace RamType0.JsonRpc
{
    /// <summary>
    /// エスケープ済みのJson内の文字列を表します。ヌル文字列はサポートされません。
    /// methodの名前の解決などに使います。
    /// </summary>
    [JsonFormatter(typeof(Formatter.Persistent))]
    public readonly struct EscapedUTF8String : IEquatable<EscapedUTF8String>, IEnumerable<byte>
    {
        readonly ArraySegment<byte> bytes;

        /// <summary>
        /// 引数で与えたUtf8を書き換えてはいけません。また、ダブルクォーテーションで囲われていない必要があります。
        /// </summary>
        /// <param name="escapedUtf8"></param>
        private EscapedUTF8String(ArraySegment<byte> escapedUtf8)
        {
            bytes = escapedUtf8;
        }
        /// <summary>
        /// 引数で与えたUtf8を書き換えてはいけません。
        /// </summary>
        public static EscapedUTF8String FromEscapedQuoted(ArraySegment<byte> text)
        {
            return new EscapedUTF8String(text.Slice(1, text.Count - 2));
            //return new EscapedUTF8String(text[1..^1]);//C#8のRange使ってみようと思ったけどArraySegment[Range]がコピー作らないかどうかがドキュメントに乗ってないので保留

        }
        public static EscapedUTF8String FromEscapedNonQuoted(ArraySegment<byte> text)
        {
            return new EscapedUTF8String(text);
        }

        public static EscapedUTF8String FromEscapedNonQuoted(ReadOnlySpan<byte> text)
        {
            var array = new byte[text.Length];
            text.CopyTo(array);
            return FromEscapedNonQuoted(array);
        }

        public static EscapedUTF8String FromUnEscaped(string text)
        {
            byte[] array = GetEscapedUTF8(text);
            return new EscapedUTF8String(array);
        }

        private static byte[] GetEscapedUTF8(string text)
        {
            var writer = new JsonWriter();
            writer.WriteString(text);
            var buffer = writer.GetBuffer();
            var array = new byte[buffer.Count - 2];
            Buffer.BlockCopy(buffer.Array!, buffer.Offset + 1, array, 0, array.Length);
            return array;
        }

        /// <summary>
        /// エスケープした状態の文字列のバイト数を示します。文字列リテラルの両端のダブルクォーテーションは含まれません。
        /// </summary>
        public int Length => bytes.Count;
        public override bool Equals(object? obj)
        {
            return obj is EscapedUTF8String @string && Equals(@string);
        }

        public bool Equals([AllowNull] EscapedUTF8String other)
        {
            return bytes.AsSpan().SequenceEqual(other.bytes.AsSpan());
        }
        /// <summary>
        /// UnEscapeした文字列を返します。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var array = GetQuoted().Array!;
            var str = new JsonReader(array).ReadString();
            ArrayPool<byte>.Shared.Return(array);
            return str;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<byte> GetQuoted()
        {
            int length = QuotedLength;
            var array = ArrayPool<byte>.Shared.Rent(length);
            var segment = new ArraySegment<byte>(array, 0, length);
            GetQuotedCore(segment);
            return segment;
        }

        private void GetQuotedCore(ArraySegment<byte> buffer)
        {
            buffer[0] = (byte)'\"';
            Buffer.BlockCopy(bytes.Array!, 0, buffer.Array!, buffer.Offset + 1, bytes.Count);
            buffer[^1] = (byte)'\"';
        }

        public int QuotedLength => Length + 2;

        public int GetQuoted(ArraySegment<byte> buffer)
        {
            int quotedLength = QuotedLength;
            if (buffer.Count >= quotedLength)
            {
                GetQuotedCore(buffer);
                return quotedLength;
            }
            else
            {
                return -1;
            }
        }
        public override int GetHashCode()
        {
            return bytes.AsSpan().GetSequenceHashCode();

        }


        private EscapedUTF8String Clone()
        {
            var array = new byte[bytes.Count];
            Buffer.BlockCopy(bytes.Array!, bytes.Offset, array, 0, array.Length);

            return EscapedUTF8String.FromEscapedNonQuoted(array);
        }


        IEnumerator<byte> IEnumerable<byte>.GetEnumerator()
        {
            return bytes.GetEnumerator();
        }

        public ReadOnlySpan<byte> Span => bytes;

        public ReadOnlySpan<byte>.Enumerator GetEnumerator() => Span.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => bytes.GetEnumerator();

        public static bool operator ==(EscapedUTF8String left, EscapedUTF8String right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EscapedUTF8String left, EscapedUTF8String right)
        {
            return !(left == right);
        }
        public static class Formatter
        {
            public static EscapedUTF8String DeserializeSafe(ref JsonReader reader)
            {
                return DeserializeUnsafe(ref reader).Clone();
            }

            public static EscapedUTF8String? DeserializeNullableSafe(ref JsonReader reader)
            {
                if (reader.ReadIsNull())
                {
                    return null;
                }
                else
                {
                    return DeserializeSafe(ref reader);
                }
            }

            public static EscapedUTF8String DeserializeUnsafe(ref JsonReader reader)
            {
                var unQuoted = reader.ReadStringSegmentRaw();
                var quoted = new ArraySegment<byte>(unQuoted.Array!, unQuoted.Offset, unQuoted.Count);
                return new EscapedUTF8String(quoted);
            }
            private static void Serialize(ref JsonWriter writer, EscapedUTF8String value)
            {
                var quotedLength = value.Length + 2;
                writer.EnsureCapacity(quotedLength);
                var writtenBuffer = writer.GetBuffer();
                var dstBuffer = writtenBuffer.Array!.AsSpan(writtenBuffer.Offset + writtenBuffer.Count);
                dstBuffer[0] = (byte)'"';
                writer.AdvanceOffset(1);
                dstBuffer = dstBuffer[1..];
                value.bytes.AsSpan().CopyTo(dstBuffer);
                writer.AdvanceOffset(value.Length);
                dstBuffer = dstBuffer[value.Length..];
                dstBuffer[0] = (byte)'"';
                writer.AdvanceOffset(1);
            }
            private static EscapedUTF8String? DeserializeNullableUnsafe(ref JsonReader reader)
            {
                if (reader.ReadIsNull())
                {
                    return null;
                }
                else
                {
                    return DeserializeUnsafe(ref reader);
                }
            }
            private static void SerializeNullable(ref JsonWriter writer, EscapedUTF8String? value)
            {
                if (value is EscapedUTF8String str)
                {
                    Serialize(ref writer, str);
                }
                else
                {
                    writer.WriteNull();
                }

            }
            public sealed class Temp : IJsonFormatter<EscapedUTF8String>
            {



                public EscapedUTF8String Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    return DeserializeUnsafe(ref reader);
                }
                /// <summary>
                /// Utf8Jsonの内部バッファをそのまま参照しているため、ヒープに保存したりすると勝手に書き換わります。即座に使用してください。
                /// </summary>
                /// <param name="reader"></param>
                /// <returns></returns>


                public void Serialize(ref JsonWriter writer, EscapedUTF8String value, IJsonFormatterResolver formatterResolver)
                {
                    Formatter.Serialize(ref writer, value);

                }


                public sealed class Nullable : IJsonFormatter<EscapedUTF8String?>
                {
                    public EscapedUTF8String? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                    {
                        return DeserializeNullableUnsafe(ref reader);
                    }



                    public void Serialize(ref JsonWriter writer, EscapedUTF8String? value, IJsonFormatterResolver formatterResolver)
                    {
                        SerializeNullable(ref writer, value);
                    }


                }
            }
            public sealed class Persistent : IJsonFormatter<EscapedUTF8String>
            {
                public EscapedUTF8String Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    return DeserializeSafe(ref reader);
                }

                public void Serialize(ref JsonWriter writer, EscapedUTF8String value, IJsonFormatterResolver formatterResolver)
                {
                    Formatter.Serialize(ref writer, value);
                }
                public sealed class Nullable : IJsonFormatter<EscapedUTF8String?>
                {
                    public EscapedUTF8String? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                    {
                        return DeserializeNullableSafe(ref reader);
                    }

                    public void Serialize(ref JsonWriter writer, EscapedUTF8String? value, IJsonFormatterResolver formatterResolver)
                    {
                        SerializeNullable(ref writer, value);
                    }
                }
            }
        }

        //[Obsolete]
        public static explicit operator EscapedUTF8String(string text)
        {
            return FromUnEscaped(text);
        }
    }
    public static class EscapedUTF8StringJsonWriterEx
    {
        public static void WriteString(ref this JsonWriter writer, EscapedUTF8String str)
        {
            writer.EnsureCapacity(str.Length + 2);
            writer.WriteRawUnsafe((byte)'\"');
            foreach (var b in str)
            {
                writer.WriteRawUnsafe(b);
            }
            writer.WriteRawUnsafe((byte)'\"');
        }
    }
}
