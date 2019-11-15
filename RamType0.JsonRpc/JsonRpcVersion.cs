using System;
using Utf8Json;
namespace RamType0.JsonRpc
{
    [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
    public readonly struct JsonRpcVersion
    {
        public sealed class Formatter : IJsonFormatter<JsonRpcVersion>
        {
            //public static Formatter Instance { get; } = new Formatter();
            public JsonRpcVersion Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                EnsureVersion(ref reader);
                return default;
            }

            private static void EnsureVersion(ref JsonReader reader)
            {
                if (!ReadIsVersion2(ref reader))
                {
                    ThrowJsonRpcFormatException();
                }

            }

            public static void ReadIsValidJsonRpcPropertyWithVerify(ref JsonReader reader)
            {
                if (ReadIsValidJsonRpcProperty(ref reader))
                {
                    return;
                }
                else
                {
                    ThrowJsonRpcFormatException();
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

            private static void ThrowJsonRpcFormatException()
            {
                throw new FormatException("JSON-RPCオブジェクトのバージョンが不正です。");
            }
        }
    }
}
