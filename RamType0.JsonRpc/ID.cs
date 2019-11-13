using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc
{
    public readonly struct ID : IEquatable<ID>
    {
        readonly private string? stringValue;
        readonly private long numberValue;
        public string? String => stringValue;
        public long? Number => stringValue is null ? numberValue : (long?)null;
        public object Value => stringValue ?? (object)numberValue;
        
        public ID(string id)
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
        internal sealed class Formatter : IJsonFormatter<ID>
        {
            //[field: ThreadStatic]
            //static Formatter? instance;
            /// <summary>
            /// 状態を持たないので複数スレッドから同時に呼ばれてもOK
            /// </summary>
            internal static Formatter Instance { get; } = new Formatter();//=> instance ??= new Formatter();
            public ID Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                switch (reader.GetCurrentJsonToken())
                {

                    case JsonToken.Number:
                        return new ID(reader.ReadInt64());
                    case JsonToken.String:
                        return new ID(reader.ReadString());
                    default:
                        throw new FormatException();
                }
            }

            public void Serialize(ref JsonWriter writer, ID value, IJsonFormatterResolver formatterResolver)
            {
                var str = value.stringValue;
                if (str is null)
                {
                    writer.WriteInt64(value.numberValue);
                }
                else
                {
                    writer.WriteString(str);
                }
            }
        }
    }
    
}
