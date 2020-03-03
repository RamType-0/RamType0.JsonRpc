using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
                reader.SkipWhiteSpace();
                var span = reader.GetBufferUnsafe().AsSpan(reader.GetCurrentOffsetUnsafe());
                switch (span.Length)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        return false;
                    default:
                        {
                            ref var spanRef = ref MemoryMarshal.GetReference(span);
                            if(Unsafe.ReadUnaligned<uint>(ref spanRef) == (('"') | ('2' << 8) | ('.' << 16) | ('0' << 24)) && Unsafe.AddByteOffset(ref spanRef,(IntPtr)4) == (byte)'"')
                            {
                                reader.AdvanceOffset(5);
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                }
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
                        if(ReadIsVersion2(ref reader))
                        {
                            return new JsonRpcVersion();
                        }
                        else
                        {
                            reader.ReadNextBlock();
                            return null;
                        }
                        
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
