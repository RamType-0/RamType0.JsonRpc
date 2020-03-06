using RamType0.JsonRpc.Client;
using RamType0.JsonRpc.Server;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Duplex
{
    using Protocol;
    using Client;
    /// <summary>
    /// 単一の入力でクライアント、サーバー双方の通信を取り持つクラスです。
    /// </summary>
    public sealed class Connection
    {
        public Connection(Server.Server server, Client client)
        {
            Server = server;
            Client = client;
        }

        public Server.Server Server { get; }
        public Client Client { get; }
        public ValueTask ResolveAsync(ArraySegment<byte> json)
        {
            var reader = new JsonReader(json.Array!, json.Offset);
            if (reader.ReadIsBeginObject())
            {

                ReadOnlySpan<byte> jsonrpcSpan = stackalloc byte[] { (byte)'j', (byte)'s', (byte)'o', (byte)'n', (byte)'r', (byte)'p', (byte)'c', },
                    versionSpan = stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0', },
                    methodSpan = stackalloc byte[] { (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d', },
                    idSpan = stackalloc byte[] { (byte)'i', (byte)'d', },
                paramsSpan = stackalloc byte[] { (byte)'p', (byte)'a', (byte)'r', (byte)'a', (byte)'m', (byte)'s', },
                resultSpan = stackalloc byte[] { (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', },
                errorSpan = stackalloc byte[] { (byte)'e', (byte)'r', (byte)'r', (byte)'o', (byte)'r', };
                bool versioned = false;
                EscapedUTF8String? methodName = null;
                ID? id = null;
                ArraySegment<byte> paramsSegment = default;
                ArraySegment<byte> resultSegment = default;
                ResponseError<object?>? _error = null;
                while (true)
                {
                    try
                    {
                        switch (reader.GetCurrentJsonToken())
                        {
                            case JsonToken.String:
                                {
                                    var propertySegmentSpan = reader.ReadPropertyNameSegmentRaw().AsSpan();
                                    if (propertySegmentSpan.SequenceEqual(jsonrpcSpan))
                                    {
                                        if (reader.ReadStringSegmentRaw().AsSpan().SequenceEqual(versionSpan))
                                        {
                                            versioned = true;
                                        }
                                        else
                                        {
                                            goto BuildMessageFailed;
                                        }
                                    }
                                    else if (propertySegmentSpan.SequenceEqual(methodSpan))
                                    {
                                        methodName = EscapedUTF8String.FromEscapedNonQuoted(reader.ReadStringSegmentRaw());
                                    }
                                    else if (propertySegmentSpan.SequenceEqual(idSpan))
                                    {
                                        id = ID.Formatter.DeserializeNullableSafe(ref reader);
                                    }
                                    else if (propertySegmentSpan.SequenceEqual(paramsSpan))
                                    {
                                        paramsSegment = reader.ReadNextBlockSegment();
                                    }else if (propertySegmentSpan.SequenceEqual(resultSpan))
                                    {
                                        resultSegment = reader.ReadNextBlockSegment();
                                    }else if (propertySegmentSpan.SequenceEqual(errorSpan))
                                    {
                                        _error = Client.JsonResolver.GetFormatterWithVerify<ResponseError<object?>>().Deserialize(ref reader, Client.JsonResolver);
                                    }

                                    if (reader.ReadIsEndObject())
                                    {
                                        goto ReachedObjectTerminal;
                                    }
                                    else
                                    {
                                        if (!reader.ReadIsValueSeparator())
                                        {
                                            goto BuildMessageFailed;
                                        }
                                        continue;
                                    }

                                }
                            case JsonToken.EndObject:
                                {
                                    reader.ReadIsEndObject();
                                    goto ReachedObjectTerminal;
                                }
                            default:
                                {
                                    goto BuildMessageFailed;
                                }
                        }
                    }
                    catch (JsonParsingException)
                    {
                        goto BuildMessageFailed;
                    }
                }
            ReachedObjectTerminal:
                if (methodName is EscapedUTF8String name)
                {
                    if (versioned)
                    {
                        if (Server.RpcMethods.TryGetValue(name, out var entry))
                        {
                            return entry.InvokeAsync(Server, paramsSegment, id);
                        }
                        else
                        {
                            if (id is ID reqID)
                                return Server.Output.ResponseError(Server,ErrorResponse.MethodNotFound(reqID, name.ToString()));
                            else return new ValueTask();
                        }
                    }
                    else
                    {
                        return Server.Output.ResponseError(Server,ErrorResponse.InvalidRequest(json));
                    }
                }
                else
                {
                    if (id is ID reqID)
                    {
                        if (Client.UnResponsedRequests.TryRemove(reqID, out var req))
                        {
                            try
                            {
                                if (_error is ResponseError<object?> error)
                                {
                                    req.SetException(Client.ErrorHandler.AsException<object>(error));
                                }
                                else
                                {
                                    req.SetResult(resultSegment, Client.JsonResolver);
                                }
                            }
                            catch (Exception e)
                            {
                                req.SetException(e);
                            }
                        }
                        else
                        {
                            throw new ArgumentException("ID conflicted!");
                        }
                    }
                    else
                    {
                        if (_error is ResponseError<object?> error)
                        {
                            Client.UnIdentifiableErrors.Add(error);
                        }
                        else
                        {
                            throw new JsonParsingException("Not a valid response or request.");
                        }
                    }
                }
            }
            else
            {
                goto BuildMessageFailed;
            }

        BuildMessageFailed:
            {
                reader = new JsonReader(json.Array!, json.Offset);
                try
                {
                    reader.ReadNextBlock();
                    reader.SkipWhiteSpace();
                    var readerOffset = reader.GetCurrentOffsetUnsafe();
                    var jsonTerminal = json.Offset + json.Count;
                    if (readerOffset >= jsonTerminal)
                    {
                        return Server.Output.ResponseError(Server,ErrorResponse.InvalidRequest(json));
                    }
                }
                catch (JsonParsingException)
                {

                }
                return Server.Output.ResponseError(Server,ErrorResponse.ParseError(json));
            }
        }

        public ValueTask<bool> TryResolveAsync(ArraySegment<byte> json)

        {
            var reader = new JsonReader(json.Array!, json.Offset);
            if (reader.ReadIsBeginObject())
            {

                bool versioned = false;
                EscapedUTF8String? methodName = null;
                ID? _id = null;
                ArraySegment<byte> paramsSegment = default;
                ResponseError<object?>? _error = null;
                ArraySegment<byte> resultSegment = default;
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
                                goto BuildMessageFailed;
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
                                        goto BuildMessageFailed;
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
                                        goto BuildMessageFailed;
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
                                                    goto BuildMessageFailed;
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
                                                    goto BuildMessageFailed;
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
                                                versioned = true;
                                                break;
                                            }
                                            else
                                            {
                                                goto BuildMessageFailed;
                                            }
                                        }
                                        else
                                        {
                                            goto BuildMessageFailed;
                                        }
                                    }
                                    else
                                    {
                                        goto BuildMessageFailed;
                                    }
                                }
                            Method:
                                {
                                    reader.AdvanceOffset(8);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        methodName = EscapedUTF8String.FromEscapedNonQuoted(reader.ReadStringSegmentRaw());
                                        break;
                                    }
                                    else
                                    {
                                        goto BuildMessageFailed;
                                    }

                                }
                            Params:
                                {
                                    reader.AdvanceOffset(8);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        paramsSegment = reader.ReadNextBlockSegment();
                                        break;
                                    }
                                    else
                                    {
                                        goto BuildMessageFailed;
                                    }

                                }
                            ID:
                                {
                                    reader.AdvanceOffset(4);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        _id = ID.Formatter.DeserializeSafe(ref reader);
                                        break;
                                    }
                                    else
                                    {
                                        goto BuildMessageFailed;
                                    }

                                }
                            Result:
                                {
                                    reader.AdvanceOffset(8);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        resultSegment = reader.ReadNextBlockSegment();
                                        break;
                                    }
                                    else
                                    {
                                        goto BuildMessageFailed;
                                    }

                                }
                            Error:
                                {
                                    reader.AdvanceOffset(7);
                                    if (reader.ReadIsNameSeparator())
                                    {
                                        _error = Client.JsonResolver.GetFormatterWithVerify<ResponseError<object?>>().Deserialize(ref reader, Client.JsonResolver);
                                        break;
                                    }
                                    else
                                    {
                                        goto BuildMessageFailed;
                                    }
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
                                goto ReachedObjectTerminal;
                            default:
                                goto BuildMessageFailed;
                        }
                    }
                }
                catch (JsonParsingException)
                {
                    goto BuildMessageFailed;
                }
            ReachedObjectTerminal:
                if (methodName is EscapedUTF8String name)
                {
                    if (versioned)
                    {
                        if (Server.RpcMethods.TryGetValue(name, out var entry))
                        {
                            return AsTrueTask(entry.InvokeAsync(Server, paramsSegment, _id));
                        }
                        else
                        {
                            if (_id is ID reqID)
                                return AsTrueTask(Server.Output.ResponseError(Server, ErrorResponse.MethodNotFound(reqID, name.ToString())));
                            else return new ValueTask<bool>(true);
                        }
                    }
                    else
                    {
                        return AsTrueTask(Server.Output.ResponseError(Server, ErrorResponse.InvalidRequest(json)));
                    }
                }
                else
                {
                    if (_id is ID id)
                    {
                        if (Client.UnResponsedRequests.TryRemove(id, out var req))
                        {
                            try
                            {
                                if (_error is ResponseError<object?> error)
                                {
                                    req.SetException(Client.ErrorHandler.AsException<object>(error));
                                }
                                else
                                {
                                    req.SetResult(resultSegment, Client.JsonResolver);
                                }
                            }
                            catch (Exception e)
                            {
                                req.SetException(e);
                            }
                        }
                        else
                        {
                            return new ValueTask<bool>(false);
                        }
                    }
                    else
                    {
                        if (_error is ResponseError<object?> error)
                        {
                            Client.UnIdentifiableErrors.Add(error);
                        }
                        else
                        {
                            return new ValueTask<bool>(false);
                        }
                    }
                }
            }
            else
            {
                goto BuildMessageFailed;
            }

        BuildMessageFailed:
            {
                reader = new JsonReader(json.Array!, json.Offset);
                try
                {
                    reader.ReadNextBlock();
                    reader.SkipWhiteSpace();
                    var readerOffset = reader.GetCurrentOffsetUnsafe();
                    var jsonTerminal = json.Offset + json.Count;
                    if (readerOffset >= jsonTerminal)
                    {
                        return AsTrueTask(Server.Output.ResponseError(Server, ErrorResponse.InvalidRequest(json)));
                    }
                }
                catch (JsonParsingException)
                {

                }
                return AsTrueTask(Server.Output.ResponseError(Server, ErrorResponse.ParseError(json)));
            }

        }

    
        static async ValueTask<bool> AsTrueTask(ValueTask task)
        {
            await task;
            return true;
        }

    }
}
