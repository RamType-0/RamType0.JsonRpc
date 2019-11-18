using System;
using Utf8Json;
namespace RamType0.JsonRpc
{
    [JsonFormatter(typeof(Formatter))]
    public readonly struct JsonRpcVersion
    {
        public sealed class Formatter : IJsonFormatter<JsonRpcVersion>
        {
            //public static Formatter Instance { get; } = new Formatter();
            public JsonRpcVersion Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                ReadIsVersion2WithVerify(ref reader);
                return default;
            }

            private static void ReadIsVersion2WithVerify(ref JsonReader reader)
            {
                var copy = reader;
                if (!ReadIsVersion2(ref reader))
                {
                    
                    ThrowJsonRpcFormatException(copy.ReadString());
                }

            }



            private static bool ReadIsValidJsonRpcProperty(ref JsonReader reader)
            {
                return ReadIsJsonRpcPropertyName(ref reader) && ReadIsVersion2(ref reader);
            }

            private static bool ReadIsJsonRpcPropertyName(ref JsonReader reader)
            {
                return reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'j', (byte)'s', (byte)'o', (byte)'n', (byte)'r', (byte)'p', (byte)'c', });
            }

            private static bool ReadIsVersion2(ref JsonReader reader)
            {
                return reader.ReadStringSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0' });
            }

            public void Serialize(ref JsonWriter writer, JsonRpcVersion value, IJsonFormatterResolver formatterResolver)
            {
                writer.EnsureCapacity(5);
                writer.WriteRawUnsafe((byte)'\"');
                writer.WriteRawUnsafe((byte)'2');
                writer.WriteRawUnsafe((byte)'.');
                writer.WriteRawUnsafe((byte)'0');
                writer.WriteRawUnsafe((byte)'\"');

            }

            private static void ThrowJsonRpcFormatException(string actualValue)
            {
                throw new JsonParsingException($"Invalid version of JsonRpc. Expected:\"2.0\" Actual : \"{actualValue}\"");
            }
            /// <summary>
            /// Deserializeを通じて正しい
            /// </summary>
            public sealed class Nullable : IJsonFormatter<JsonRpcVersion?>
            {
                public JsonRpcVersion? Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    if (reader.ReadIsNull())
                    {
                        return null;
                    }
                    else
                    {
                        ReadIsVersion2WithVerify(ref reader);
                        return default(JsonRpcVersion);
                    }
                }

                public void Serialize(ref JsonWriter writer, JsonRpcVersion? value, IJsonFormatterResolver formatterResolver)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
