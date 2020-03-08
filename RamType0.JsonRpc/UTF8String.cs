using RamType0.JsonRpc.Internal;
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Utf8Json;
namespace RamType0.JsonRpc
{
    [JsonFormatter(typeof(Formatter.Persistent))]
    public readonly struct UTF8String : IEquatable<UTF8String>
    {
        readonly ArraySegment<byte> bytes;

        public UTF8String(ArraySegment<byte> bytes)
        {
            this.bytes = bytes;
        }

        public UTF8String(string str)
        {
            bytes = Encoding.UTF8.GetBytes(str);
        }

        public ReadOnlySpan<byte> Span => bytes.AsSpan();

        private static UTF8String UnEscape(EscapedUTF8String escaped)
        {
            bool shouldReturn = UnEscapeUnsafe(escaped, out var str);

            UTF8String ret = str.Clone();
            if (shouldReturn)
            {

                ArrayPool<byte>.Shared.Return(str.bytes.Array!);


            }
            return ret;
        }

        /// <summary>
        /// trueを返した場合、<see cref="ArrayPool{byte}.Shared"/>に内部バッファを返還する必要があり、また返還するまでは永久に使用可能です。
        /// falseを返した場合、<see cref="ArrayPool{byte}.Shared"/>に内部バッファを返還する必要はありませんが、ヒープに保存することができません。
        /// </summary>
        /// <param name="escaped"></param>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool UnEscapeUnsafe(EscapedUTF8String escaped, out UTF8String str)
        {
            var quoted = escaped.GetQuoted();
            var reader = new JsonReader(quoted.Array, quoted.Offset);

            var segment = reader.ReadStringSegmentUnsafe();
            str = new UTF8String(segment);
            if (quoted.Array == segment.Array)
            {
                return true;
            }
            else
            {
                ArrayPool<byte>.Shared.Return(quoted.Array!);
                return false;
            }
        }

        private UTF8String Clone()
        {
            var array = new byte[bytes.Count];
            Buffer.BlockCopy(bytes.Array!, bytes.Offset, array, 0, array.Length);
            return new UTF8String(array);
        }

        public override bool Equals(object? obj)
        {
            return obj is UTF8String @string && Equals(@string);
        }

        public bool Equals([AllowNull] UTF8String other)
        {
            return bytes.AsSpan().SequenceEqual(other.bytes.AsSpan());
        }

        public override int GetHashCode()
        {
            return bytes.AsSpan().GetSequenceHashCode();
        }
        public static explicit operator UTF8String(EscapedUTF8String escaped)
        {
            return UnEscape(escaped);
        }

        public static bool operator ==(UTF8String left, UTF8String right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UTF8String left, UTF8String right)
        {
            return !(left == right);
        }

        public static class Formatter
        {
            public static void Serialize(ref JsonWriter writer, UTF8String value)
            {
                int noEscapedLength = value.Span.Length + 2;
                writer.EnsureCapacity(noEscapedLength);
                writer.WriteRawUnsafe((byte)'\"');
                int bytesWritten = 1;
                foreach (var item in value.Span)
                {
                    byte escapeChar;// = default;
                    switch (item)
                    {
                        case (byte)'\"': escapeChar = (byte)'\"'; goto Escape;
                        case (byte)'\\': escapeChar = (byte)'\\'; goto Escape;
                        case (byte)'\b': escapeChar = (byte)'b'; goto Escape;
                        case (byte)'\f': escapeChar = (byte)'f'; goto Escape;
                        case (byte)'\n': escapeChar = (byte)'n'; goto Escape;
                        case (byte)'\r': escapeChar = (byte)'r'; goto Escape;
                        case (byte)'\t': escapeChar = (byte)'t'; goto Escape;
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                        case 6:
                        case 7:
                        case 11:
                        case 14:
                        case 15:
                        case 16:
                        case 17:
                        case 18:
                        case 19:
                        case 20:
                        case 21:
                        case 22:
                        case 23:
                        case 24:
                        case 25:
                        case 26:
                        case 27:
                        case 28:
                        case 29:
                        case 30:
                        case 31:
                        case 32:
                        case 33:
                        case 35:
                        case 36:
                        case 37:
                        case 38:
                        case 39:
                        case 40:
                        case 41:
                        case 42:
                        case 43:
                        case 44:
                        case 45:
                        case 46:
                        case 47:
                        case 48:
                        case 49:
                        case 50:
                        case 51:
                        case 52:
                        case 53:
                        case 54:
                        case 55:
                        case 56:
                        case 57:
                        case 58:
                        case 59:
                        case 60:
                        case 61:
                        case 62:
                        case 63:
                        case 64:
                        case 65:
                        case 66:
                        case 67:
                        case 68:
                        case 69:
                        case 70:
                        case 71:
                        case 72:
                        case 73:
                        case 74:
                        case 75:
                        case 76:
                        case 77:
                        case 78:
                        case 79:
                        case 80:
                        case 81:
                        case 82:
                        case 83:
                        case 84:
                        case 85:
                        case 86:
                        case 87:
                        case 88:
                        case 89:
                        case 90:
                        case 91:
                        default:
                            writer.WriteRawUnsafe(item);
                            bytesWritten++;
                            continue;

                    }
                Escape:;
                    writer.EnsureCapacity(noEscapedLength + 1 - bytesWritten);
                    writer.WriteRawUnsafe((byte)'\\');
                    writer.WriteRawUnsafe(escapeChar);
                    bytesWritten += 2;

                }
                writer.WriteRawUnsafe((byte)'\"');
                return;
            }

            public static UTF8String DeserializeUnsafe(ref JsonReader reader)
            {
                return new UTF8String(reader.ReadStringSegmentUnsafe());
            }
            public static UTF8String DeserializeSafe(ref JsonReader reader)
            {
                return DeserializeUnsafe(ref reader).Clone();
            }
            public sealed class Persistent : IJsonFormatter<UTF8String>
            {
                public UTF8String Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    return DeserializeSafe(ref reader);
                }

                public void Serialize(ref JsonWriter writer, UTF8String value, IJsonFormatterResolver formatterResolver)
                {
                    Formatter.Serialize(ref writer, value);
                }
            }
            public sealed class Temp : IJsonFormatter<UTF8String>
            {
                public UTF8String Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    return DeserializeUnsafe(ref reader);
                }

                public void Serialize(ref JsonWriter writer, UTF8String value, IJsonFormatterResolver formatterResolver)
                {
                    Formatter.Serialize(ref writer, value);
                }
            }
        }

    }
}
