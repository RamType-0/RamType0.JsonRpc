using System;
using Utf8Json;
namespace RamType0.JsonRpc
{
    readonly struct JsonRpcVersion
    {
        public sealed class Formatter : IJsonFormatter<JsonRpcVersion>
        {
            public static Formatter Instance { get; } = new Formatter();
            public JsonRpcVersion Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                EnsureVersion(ref reader);
                return default;
            }

            private static JsonReader EnsureVersion(ref JsonReader reader)
            {
                if (!reader.ReadStringSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0' }))
                {
                    ThrowJsonRpcFormatException();
                }

                return reader;
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
