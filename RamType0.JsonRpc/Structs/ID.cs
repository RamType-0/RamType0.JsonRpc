using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc
{
    [JsonFormatter(typeof(ID.Formatter))]
    public readonly struct ID : IEquatable<ID>
    {
        readonly private long numberValue;
        public EscapedUTF8String? String { get; }
        public long? Number => !String.HasValue ? numberValue : (long?)null;
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

            public void Serialize(ref JsonWriter writer, ID value, IJsonFormatterResolver formatterResolver)
            {
                Serialize(ref writer, value);
            }

            public static void Serialize(ref JsonWriter writer, ID value)
            {
                
                if(value.String is EscapedUTF8String str)
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
                return (reader.GetCurrentJsonToken()) switch
                {
                JsonToken.Number => new ID(reader.ReadInt64()),
                JsonToken.String => new ID(EscapedUTF8String.Formatter.DeserializeUnsafe(ref reader)), // new ID(formatterResolver.GetFormatter<EscapedUTF8String.Formatter>().Deserialize(ref reader, formatterResolver)),
                    JsonToken.Null => (ID?)null,
                    _ => throw new JsonParsingException("Expected number or string or null"),
                };
            }

            public static ID? DeserializeNullableSafe(ref JsonReader reader)
            {
                return (reader.GetCurrentJsonToken()) switch
                {
                    JsonToken.Number => new ID(reader.ReadInt64()),
                    JsonToken.String => new ID(EscapedUTF8String.Formatter.DeserializeSafe(ref reader)), // new ID(formatterResolver.GetFormatter<EscapedUTF8String.Formatter>().Deserialize(ref reader, formatterResolver)),
                    JsonToken.Null => (ID?)null,
                    _ => throw new JsonParsingException("Expected number or string or null"),
                };
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
    
}
