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
        readonly private EscapedUTF8String? stringValue;
        readonly private long numberValue;
        public EscapedUTF8String? String => stringValue;
        public long? Number => stringValue is null ? numberValue : (long?)null;
        public object Value => stringValue ?? (object)numberValue;
        
        public ID(EscapedUTF8String id)
        {
            stringValue = id;
            numberValue = default;
        }

        public ID(long id)
        {
            numberValue = id;
            stringValue = null;
        }

        public override bool Equals(object? obj)
        {
            return obj is ID iD && Equals(iD);
        }

        public bool Equals([AllowNull] ID other)
        {
            if (stringValue is null)
            {
                return other.stringValue is null & numberValue == other.numberValue;
            }
            else
            {
                return stringValue == other.stringValue;
            }
        }

        public override int GetHashCode()
        {
            if(stringValue is null)
            {
                return numberValue.GetHashCode();
            }
            else
            {
                return stringValue.GetHashCode();
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
                switch (reader.GetCurrentJsonToken())
                {

                    case JsonToken.Number:
                        return new ID(reader.ReadInt64());
                    case JsonToken.String:
                        return new ID(formatterResolver.GetFormatter<EscapedUTF8String>().Deserialize(ref reader,formatterResolver));
                    default:
                        throw new JsonParsingException("Expected number or string");
                }
            }

            public void Serialize(ref JsonWriter writer, ID value, IJsonFormatterResolver formatterResolver)
            {
                Serialize(ref writer, value);
            }

            public static void Serialize(ref JsonWriter writer, ID value)
            {
                
                if(value.stringValue is EscapedUTF8String str)
                {
                    writer.WriteString(str);
                }
                else
                {
                    writer.WriteInt64(value.numberValue);
                }
            }
            public sealed class Nullable : IJsonFormatter<ID?>
            {

                public ID? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    switch (reader.GetCurrentJsonToken())
                    {

                        case JsonToken.Number:
                            return new ID(reader.ReadInt64());
                        case JsonToken.String:
                            return new ID(formatterResolver.GetFormatter<EscapedUTF8String>().Deserialize(ref reader, formatterResolver));
                        case JsonToken.Null:
                            return null;
                        default:
                            throw new JsonParsingException("Expected number or string or null");
                    }
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
