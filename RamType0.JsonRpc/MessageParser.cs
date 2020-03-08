
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Utf8Json;

namespace RamType0.JsonRpc
{
    public static class MessageParser
    {
        public static MessageParseResult ParseDuplexMessage(ArraySegment<byte> message)
        {
            var parseResult = new MessageParseResult();
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
                                                parseResult[MessagePropertyKind.JsonRpcVersion] = PropertyState.Valid;
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
                                            parseResult.Method = EscapedUTF8String.FromEscapedNonQuoted(reader.ReadStringSegmentRaw());
                                        }
                                        catch (JsonParsingException)
                                        {
                                            parseResult[MessagePropertyKind.Method] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        parseResult[MessagePropertyKind.Method] = PropertyState.Valid;
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
                                            parseResult.Params = reader.ReadNextBlockSegment();
                                        }
                                        catch (JsonParsingException)
                                        {
                                            parseResult[MessagePropertyKind.Params] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        parseResult[MessagePropertyKind.Params] = PropertyState.Valid;
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
                                            parseResult.id = ID.Formatter.DeserializeNullableSafe(ref reader);
                                        }
                                        catch (JsonParsingException)
                                        {
                                            parseResult[MessagePropertyKind.ID] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        parseResult[MessagePropertyKind.ID] = PropertyState.Valid;
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
                                            parseResult.Result = reader.ReadNextBlockSegment();
                                        }
                                        catch (JsonParsingException)
                                        {
                                            parseResult[MessagePropertyKind.Result] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        parseResult[MessagePropertyKind.Result] = PropertyState.Valid;
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
                                            parseResult.Error = reader.ReadNextBlockSegment(); //Client.JsonResolver.GetFormatterWithVerify<Client.ResponseError<object?>>().Deserialize(ref reader, Client.JsonResolver);
                                        }
                                        catch (JsonParsingException)
                                        {
                                            parseResult[MessagePropertyKind.Error] = PropertyState.Invalid;
                                            goto UnExpectedFormatProperty;
                                        }
                                        parseResult[MessagePropertyKind.Error] = PropertyState.Valid;
                                        break;
                                    }
                                    else
                                    {
                                        goto InvalidJson;
                                    }
                                }
                            UnknownProperty:
                                {
                                    parseResult.ParseErrors |= MessageParseErrors.HasUnknownProperty;
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
                                    parseResult.ParseErrors = MessageParseErrors.HasInvalidProperty;
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
                                    return parseResult;
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
                parseResult.ParseErrors |= MessageParseErrors.IsNotAObject;
            }

        InvalidJson:
            {
                parseResult.ParseErrors |= MessageParseErrors.IsInvalidJson;
            }
            return parseResult;

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
                var writtenBuffer = writer.GetBuffer();
                var buffer = writtenBuffer.Array!.AsSpan(writtenBuffer.Offset + writtenBuffer.Count);
                span.CopyTo(buffer);
                writer.AdvanceOffset(span.Length);
            }
        }
    }
    public unsafe struct MessageParseResult
    {
        fixed byte props[8];
        internal ref ulong Bits
        {
            get
            {
                return ref Unsafe.As<byte, ulong>(ref props[0]);
            }
        }

        public ref MessageParseErrors ParseErrors
        {
            get
            {
                return ref Unsafe.As<byte, MessageParseErrors>(ref props[7]);
            }

        }

        public MessageKind MessageKind
        {
            get
            {
                if (HasParseError)
                {
                    if ((ParseErrors & MessageParseErrors.IsInvalidJson) != 0)
                    {
                        return MessageKind.InvalidJson;
                    }
                    else
                    {
                        return MessageKind.InvalidMessage;
                    }
                }
                else
                {
                    if (HasInvalidPropertyOrMissingVersion)
                    {
                        return MessageKind.InvalidMessage;
                    }
                    else
                    {


                        if (this[MessagePropertyKind.Method] == PropertyState.Missing)
                        {
                            if (IsValidResultResponse)
                            {
                                return MessageKind.ResultResponse;
                            }
                            else if (IsValidErrorResponse)
                            {
                                return MessageKind.ErrorResponse;
                            }
                            else
                            {
                                return MessageKind.InvalidMessage;
                            }
                        }
                        else
                        {
                            if (IsValidClientMessage)
                            {
                                return MessageKind.ClientMessage;
                            }
                            else
                            {
                                return MessageKind.InvalidMessage;
                            }
                        }



                    }

                }
            }
        }

        public bool HasParseError => ParseErrors != 0;
        bool HasInvalidPropertyOrMissingVersion => (Bits & 0x0101010101FF) != 2;
        public bool IsValidClientMessage => (Bits & 0x000001FF01FF) == 0x000000020002;
        public bool IsValidRequest => (Bits & 0x000001FFFFFF) == 0x000000020202;
        public bool IsValidNotification => (Bits & 0x000001FFFFFF) == 0x000000020002;
        public bool IsValidResultResponse => (Bits & 0xFFFF0000FFFF) == 0x000200000202;
        public bool IsValidErrorResponse => (Bits & 0xFFFF0000FFFF) == 0x020000000202;
        public PropertyState this[MessagePropertyKind propertyKind]
        {
            get
            {
                return (PropertyState)props[(byte)propertyKind];
            }
            set
            {
                props[(byte)propertyKind] = (byte)value;
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

    public enum PropertyState : byte
    {
        Missing,
        Invalid,
        Valid,
    }
    [Flags]
    public enum MessageParseErrors : byte
    {
        HasUnknownProperty = 1,
        IsNotAObject = 2,
        IsInvalidJson = 4,
        HasInvalidProperty = 8,

    }

    public enum MessageKind
    {
        InvalidJson,
        InvalidMessage,
        ClientMessage,
        ResultResponse,
        ErrorResponse

    }

}
