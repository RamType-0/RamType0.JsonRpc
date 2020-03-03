using RamType0.JsonRpc.Server;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using Utf8Json;
using Utf8Json.Resolvers;
using Utf8Json.Formatters;
using System.Collections.Specialized;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.Intrinsics.X86;
namespace RamType0.JsonRpc
{
    public static class MessageParser
    {
        public static MessageParseResult ParseDuplexMessage(ArraySegment<byte> message)
        {
            var duplexMessage = new MessageParseResult();
            var reader = new JsonReader(message.Array!, message.Offset);
            if (reader.ReadIsBeginObject())
            {
                
                try
                {
                    while (true)
                    {
                        reader.SkipWhiteSpace();
                        var buffer = reader.GetBufferUnsafe().AsSpan(reader.GetCurrentOffsetUnsafe());
                        ref var bufferRef = ref MemoryMarshal.GetReference(buffer);
                        const uint id = (('"') | ('i' << 8) | ('d' << 16) | ('"' << 24));
                        const ulong method = ((ulong)'"') | ((ulong)'m' << 8) | ((ulong)'e' << 16) | ((ulong)'t' << 24) | ((ulong)'h' << 32) | ((ulong)'o' << 40) | ((ulong)'d' << 48) | ((ulong)'"' << 56);
                        const ulong @params = ((ulong)'"') | ((ulong)'p' << 8) | ((ulong)'a' << 16) | ((ulong)'r' << 24) | ((ulong)'a' << 32) | ((ulong)'m' << 40) | ((ulong)'s' << 48) | ((ulong)'"' << 56);
                        const ulong jsonrpc = (((ulong)'"') | ((ulong)'j' << 8) | ((ulong)'s' << 16) | ((ulong)'o' << 24) | ((ulong)'n' << 32) | ((ulong)'r' << 40) | ((ulong)'p' << 48) | ((ulong)'c' << 56));
                        const ulong ___2_0 = (((ulong)'"') | ((ulong)'2' << 8) | ((ulong)'.' << 16) | ((ulong)'0' << 24) | ((ulong)'"' << 32)) << 24;
                        const ulong result = (((ulong)'"') | ((ulong)'r' << 8) | ((ulong)'e' << 16) | ((ulong)'s' << 24) | ((ulong)'u' << 32) | ((ulong)'l' << 40) | ((ulong)'t' << 48) | ((ulong)'"' << 56));
                        const ulong error = (((ulong)'"') | ((ulong)'e' << 8) | ((ulong)'r' << 16) | ((ulong)'r' << 24) | ((ulong)'o' << 32) | ((ulong)'r' << 40) | ((ulong)'"' << 48));

                        switch (buffer.Length)
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                            case 4:
                            case 5:
                            case 6:
                                goto UnknownProperty;
                            //最短の正常な文字列パターンは"id":1}で7byteある
                            case 7:
                            case 8:
                            case 9:
                            case 10:

                                {
                                    if (Unsafe.ReadUnaligned<uint>(ref bufferRef) == id)
                                    {
                                        goto ID;
                                    }
                                    else
                                    {
                                        goto UnknownProperty;
                                    }
                                }
                            case 11:
                                {
                                    var chars8 = Unsafe.ReadUnaligned<ulong>(ref bufferRef);

                                    if ((uint)chars8 == id)
                                    {
                                        goto ID;

                                    }
                                    else if ((chars8 & (ulong)0x00FFFFFFFFFFFFFF) == error)
                                    {
                                        goto Error;
                                    }
                                    else
                                    {
                                        goto UnknownProperty;
                                    }


                                }
                            //その次に短いのは"params":[]}または"params":{}}または"method":""または"result":1}
                            case 12:
                            case 13:
                            case 14:
                            case 15:
                                {
                                    var chars8 = Unsafe.ReadUnaligned<ulong>(ref bufferRef);
                                    switch (chars8)
                                    {
                                        case result:
                                            {
                                                goto Result;
                                            }
                                        case method:
                                            {
                                                goto Method;

                                            }
                                        case @params:
                                            {
                                                goto Params;
                                            }
                                        default:
                                            {
                                                if ((uint)chars8 == id)
                                                {
                                                    goto ID;

                                                }
                                                else if ((chars8 & (ulong)0x00FFFFFFFFFFFFFF) == error)
                                                {
                                                    goto Error;
                                                }
                                                else
                                                {
                                                    goto UnknownProperty;
                                                }
                                            }
                                    }
                                }
                            //その次が"jsonrpc":"2.0"}
                            default:
                                {
                                    var chars8 = Unsafe.ReadUnaligned<ulong>(ref bufferRef);
                                    switch (chars8)
                                    {
                                        case jsonrpc:
                                            {
                                                goto JsonRpc;
                                            }
                                        case result:
                                            {
                                                goto Result;
                                            }
                                        case method:
                                            {
                                                goto Method;

                                            }
                                        case @params:
                                            {
                                                goto Params;
                                            }
                                        default:
                                            {
                                                if ((uint)chars8 == id)
                                                {
                                                    goto ID;

                                                }
                                                else if ((chars8 & (ulong)0x00FFFFFFFFFFFFFF) == error)
                                                {
                                                    goto Error;
                                                }
                                                else
                                                {
                                                    goto UnknownProperty;
                                                }
                                            }
                                    }
                                }
                            JsonRpc:
                                {
                                    if (Unsafe.AddByteOffset(ref bufferRef, (IntPtr)(8)) == (byte)'"')
                                    {
                                        reader.AdvanceOffset(9);
                                        if (reader.ReadIsNameSeparator())
                                        {
                                            reader.SkipWhiteSpace();
                                            buffer = reader.GetBufferUnsafe().AsSpan(reader.GetCurrentOffsetUnsafe());
                                            bufferRef = ref MemoryMarshal.GetReference(buffer);
                                            if ((Unsafe.ReadUnaligned<ulong>(ref Unsafe.AddByteOffset(ref bufferRef, (IntPtr)(-3))) & 0xFFFFFFFFFF000000) == ___2_0)
                                            {
                                                reader.AdvanceOffset(5);
                                                duplexMessage[MessagePropertyKind.JsonRpcVersion] = PropertyState.Valid;
                                                break;
                                            }
                                            else
                                            {
                                                goto UnExpectedFormatProperty;
                                            }
                                        }
                                        else
                                        {
                                            goto InvalidJson;
                                        }
                                    }
                                    else
                                    {
                                        goto UnknownProperty;
                                    }
                                }
                            Method:
                                {
                                    reader.AdvanceOffset(8);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        try
                                        {
                                            duplexMessage.Method = EscapedUTF8String.FromEscapedNonQuoted(reader.ReadStringSegmentRaw());
                                        }
                                        catch (JsonParsingException)
                                        {
                                            duplexMessage[MessagePropertyKind.Method] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        duplexMessage[MessagePropertyKind.Method] = PropertyState.Valid;
                                        break;
                                    }
                                    else
                                    {
                                        goto InvalidJson;
                                    }

                                }
                            Params:
                                {
                                    reader.AdvanceOffset(8);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        try
                                        {
                                            duplexMessage.Params = reader.ReadNextBlockSegment();
                                        }
                                        catch (JsonParsingException)
                                        {
                                            duplexMessage[MessagePropertyKind.Params] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        duplexMessage[MessagePropertyKind.Params] = PropertyState.Valid;
                                        break;
                                    }
                                    else
                                    {
                                        goto InvalidJson;
                                    }

                                }
                            ID:
                                {
                                    reader.AdvanceOffset(4);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        try
                                        {
                                            duplexMessage.id = ID.Formatter.DeserializeSafe(ref reader);
                                        }
                                        catch (JsonParsingException)
                                        {
                                            duplexMessage[MessagePropertyKind.ID] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        duplexMessage[MessagePropertyKind.ID] = PropertyState.Valid;
                                        break;
                                    }
                                    else
                                    {
                                        goto InvalidJson;
                                    }

                                }
                            Result:
                                {
                                    reader.AdvanceOffset(8);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        try
                                        {
                                            duplexMessage.Result = reader.ReadNextBlockSegment();
                                        }
                                        catch (JsonParsingException)
                                        {
                                            duplexMessage[MessagePropertyKind.Result] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        duplexMessage[MessagePropertyKind.Result] = PropertyState.Valid;
                                        break;
                                    }
                                    else
                                    {
                                        goto InvalidJson;
                                    }

                                }
                            Error:
                                {
                                    reader.AdvanceOffset(7);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        try
                                        {
                                            duplexMessage.Error = reader.ReadNextBlockSegment(); //Client.JsonResolver.GetFormatterWithVerify<Client.ResponseError<object?>>().Deserialize(ref reader, Client.JsonResolver);
                                        }
                                        catch (JsonParsingException)
                                        {
                                            duplexMessage[MessagePropertyKind.Error] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        duplexMessage[MessagePropertyKind.Error] = PropertyState.Valid;
                                        break;
                                    }
                                    else
                                    {
                                        goto InvalidJson;
                                    }
                                }
                            UnknownProperty:
                                {
                                    duplexMessage.State |= MessageParseInfos.HasUnknownProperty;
                                    try
                                    {
                                        reader.ReadStringSegmentRaw();
                                    }
                                    catch (JsonParsingException)
                                    {
                                        goto InvalidJson;
                                    }

                                    if (reader.ReadIsValueSeparator())
                                    {
                                        try
                                        {
                                            reader.ReadNextBlock();
                                        }
                                        catch (JsonParsingException)
                                        {
                                            goto InvalidJson;
                                        }
                                        break;
                                    }
                                    else
                                    {
                                        goto InvalidJson;
                                    }
                                    
                                }
                            UnExpectedFormatProperty:
                                {
                                    try
                                    {
                                        reader.ReadNextBlock();
                                    }
                                    catch (JsonParsingException)
                                    {
                                        goto InvalidJson;
                                    }
                                    break;
                                }
                            

                        }
                        switch (reader.GetCurrentJsonToken())
                        {
                            case JsonToken.ValueSeparator:
                                {
                                    reader.AdvanceOffset(1);
                                    continue;
                                }
                            case JsonToken.EndObject:
                                {
                                    return duplexMessage;
                                }
                                
                            default:
                                goto InvalidJson;
                        }
                    }
                }
                catch (JsonParsingException)
                {
                    goto UnknownParseError;
                }
            }
            else
            {
                goto UnknownParseError;
            }


        UnknownParseError:
            {
                reader = new JsonReader(message.Array!, message.Offset);
                try
                {
                    reader.ReadNextBlock();
                }
                catch (JsonParsingException)
                {
                    goto InvalidJson;
                }
                reader.SkipWhiteSpace();
                if (reader.GetCurrentOffsetUnsafe() < message.Offset + message.Count)
                {
                    goto InvalidJson;
                }
                else
                {
                    goto NotAObject;
                }
            }

        NotAObject:
            {
                duplexMessage.State |= MessageParseInfos.IsNotAObject;
            }

        InvalidJson:
            {
                duplexMessage.State |= MessageParseInfos.HasParseError;
            }
            return duplexMessage;

        }
        public sealed class SegmentExtractFormatter : IJsonFormatter<ArraySegment<byte>>
        {
            public ArraySegment<byte> Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                return reader.ReadNextBlockSegment();
            }

            public void Serialize(ref JsonWriter writer, ArraySegment<byte> value, IJsonFormatterResolver formatterResolver)
            {
                var span = value.AsSpan();
                writer.EnsureCapacity(span.Length);
                span.CopyTo(writer.GetBuffer().AsSpan());
                writer.AdvanceOffset(span.Length);
            }
        }
    }

    [StructLayout(LayoutKind.Auto)]
    public struct MessageParseResult
    {
        uint bits;
        const byte PropertyStateSize = 2;
        const uint PropertyStateMask = (1 << PropertyStateSize) - 1;
        const byte MessageStateSize = 4;


        public MessageParseInfos State
        {
            get
            {
                return (MessageParseInfos)(bits >> (32 - MessageStateSize));
            }
            set
            {
                const uint Mask = (uint)((1 << MessageStateSize) - 1) << (32 - MessageStateSize);
                bits = (bits & ~Mask) | (uint)value << (32 - MessageStateSize);
            }
        }

        public PropertyState this[MessagePropertyKind propertyKind]
        {
            get
            {
                return (PropertyState)BitFieldExtract(bits, (byte)propertyKind);
            }
            set
            {
                var mask = PropertyStateMask << (int)propertyKind;
                bits = (bits & ~mask) | (uint)value << (int)propertyKind;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint BitFieldExtract(uint value, byte start)
        {

            if (Bmi1.IsSupported)
            {
                return Bmi1.BitFieldExtract(value, start, PropertyStateSize);
            }
            else
            {
                var mask = PropertyStateMask << start;
                return (value & mask) >> start;
            }
        }
        //[DataMember(Name = "jsonrpc")]
        //[JsonFormatter(typeof(JsonRpcVersion.Formatter.Nullable))]
        //public JsonRpcVersion? Version;
        [DataMember(Name = "id")]
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID? id;
        /// <summary>
        /// このフィールドのデータはパース元のJsonのバイナリデータを直接参照しています。
        /// </summary>
        [DataMember(Name = "method")]
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp))]
        public EscapedUTF8String Method;
        /// <summary>
        /// このフィールドのデータはパース元のJsonのバイナリデータを直接参照しています。
        /// </summary>
        [DataMember(Name = "params")]
        [JsonFormatter(typeof(MessageParser.SegmentExtractFormatter))]
        public ArraySegment<byte> Params;
        /// <summary>
        /// このフィールドのデータはパース元のJsonのバイナリデータを直接参照しています。
        /// </summary>
        [DataMember(Name = "result")]
        [JsonFormatter(typeof(MessageParser.SegmentExtractFormatter))]
        public ArraySegment<byte> Result;
        /// <summary>
        /// このフィールドのデータはパース元のJsonのバイナリデータを直接参照しています。
        /// </summary>
        [DataMember(Name = "error")]
        [JsonFormatter(typeof(MessageParser.SegmentExtractFormatter))]
        public ArraySegment<byte> Error;
        
        
    }


    public enum MessagePropertyKind
    {
        JsonRpcVersion,
        ID,
        Method,
        Params,
        Result,
        Error

    }

    public enum PropertyState
    {
        Missing,
        Invalid,
        Valid,
    }
    [Flags]
    public enum MessageParseInfos
    {
        HasUnknownProperty = 1,
        IsNotAObject = 2,
        HasParseError = 4

    }

}
