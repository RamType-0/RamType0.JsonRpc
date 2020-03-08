using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using Utf8Json;

namespace RamType0.JsonRpc
{
    [JsonFormatter(typeof(ID.Formatter))]
    public readonly struct ID : IEquatable<ID>
    {

        readonly private long numberValue;
        public EscapedUTF8String? String { get; }
        public long? Number => String.HasValue ? (long?)null : numberValue;
        public object Value => String ?? (object)numberValue;

        public ID(EscapedUTF8String id)
        {
            String = id;
            numberValue = default;
        }

        public ID(long id)
        {
            numberValue = id;
            String = null;
        }

        public override bool Equals(object? obj)
        {
            return obj is ID iD && Equals(iD);
        }

        public bool Equals([AllowNull] ID other)
        {
            if (String.HasValue)
            {
                return String == other.String;
            }
            else
            {
                return !other.String.HasValue & numberValue == other.numberValue;
            }
        }

        public override int GetHashCode()
        {
            if (String.HasValue)
            {
                return String.GetHashCode();
            }
            else
            {
                return numberValue.GetHashCode();
            }
        }

        public override string ToString()
        {
            if (String is EscapedUTF8String str)
            {
                return Encoding.UTF8.GetString(str.Span);
            }
            else
            {
                return numberValue.ToString();
            }
        }

        public static bool operator ==(ID left, ID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ID left, ID right)
        {
            return !(left == right);
        }

        

        public sealed class Formatter : IJsonFormatter<ID>
        {

            public ID Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                return (reader.GetCurrentJsonToken()) switch
                {
                    JsonToken.Number => new ID(reader.ReadInt64()),
                    JsonToken.String => new ID(EscapedUTF8String.Formatter.DeserializeSafe(ref reader)),
                    _ => throw new JsonParsingException("Expected number or string"),
                };
            }

            public static ID DeserializeSafe(ref JsonReader reader)
            {
                return (reader.GetCurrentJsonToken()) switch
                {
                    JsonToken.Number => new ID(reader.ReadInt64()),
                    JsonToken.String => new ID(EscapedUTF8String.Formatter.DeserializeSafe(ref reader)),
                    _ => throw new JsonParsingException("Expected number or string"),
                };
            }
            public static ID DeserializeUnsafe(ref JsonReader reader)
            {
                return (reader.GetCurrentJsonToken()) switch
                {
                    JsonToken.Number => new ID(reader.ReadInt64()),
                    JsonToken.String => new ID(EscapedUTF8String.Formatter.DeserializeUnsafe(ref reader)),
                    _ => throw new JsonParsingException("Expected number or string"),
                };
            }

            public void Serialize(ref JsonWriter writer, ID value, IJsonFormatterResolver formatterResolver)
            {
                Serialize(ref writer, value);
            }

            public static void Serialize(ref JsonWriter writer, ID value)
            {

                if (value.String is EscapedUTF8String str)
                {
                    writer.WriteString(str);
                }
                else
                {
                    writer.WriteInt64(value.numberValue);
                }
            }

            public static ID? DeserializeNullableUnsafe(ref JsonReader reader)
            {
                switch (reader.GetCurrentJsonToken())
                {
                    case JsonToken.Number: return new ID(reader.ReadInt64());
                    case JsonToken.String: return new ID(EscapedUTF8String.Formatter.DeserializeUnsafe(ref reader)); // new ID(formatterResolver.GetFormatter<EscapedUTF8String.Formatter>().Deserialize(ref reader, formatterResolver)),
                    case JsonToken.Null:
                        {
                            reader.AdvanceOffset(4);
                            return null;
                        }
                    default:
                        {
                            return ThrowUnExpected();
                        }

                };
            }

            public static ID? DeserializeNullableSafe(ref JsonReader reader)
            {

                switch (reader.GetCurrentJsonToken())
                {
                    case JsonToken.Number: return new ID(reader.ReadInt64());
                    case JsonToken.String: return new ID(EscapedUTF8String.Formatter.DeserializeSafe(ref reader)); // new ID(formatterResolver.GetFormatter<EscapedUTF8String.Formatter>().Deserialize(ref reader, formatterResolver)),
                    case JsonToken.Null:
                        {
                            reader.AdvanceOffset(4);
                            return null;
                        }
                    default:
                        {
                            return ThrowUnExpected();
                        }

                };
            }
            [DoesNotReturn]
            private static ID? ThrowUnExpected()
            {
                throw new JsonParsingException("Expected number or string or null");
            }

            public sealed class Nullable : IJsonFormatter<ID?>
            {

                public ID? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    return DeserializeNullableSafe(ref reader);
                }



                public void Serialize(ref JsonWriter writer, ID? value, IJsonFormatterResolver formatterResolver)
                {
                    if (value is ID id)
                    {
                        Formatter.Serialize(ref writer, id);
                    }
                    else
                    {
                        writer.WriteNull();
                    }
                }
            }
        }


    }

    public static class NullableIDExtension
    {
        /// <summary>
        /// このメソッドは<see cref="RpcDomain.ResolveMessagesAsync"/>から呼び出された際、<see cref="RpcIDAttribute"/>によって受け取った<see cref="ID?"/>から一度だけ呼び出すことができます。
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static CancellationToken AsRpcCancellationToken(this ID? id)
        {
            if(id is ID reqID)
            {
                return RpcDomain.AllocCancellationToken(reqID);
            }
            else
            {
                return CancellationToken.None;
            }
            
        }
    }

}
