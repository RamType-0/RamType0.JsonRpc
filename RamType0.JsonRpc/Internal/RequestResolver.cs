using RamType0.JsonRpc.Server;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    public sealed class RequestResolver
    {
        internal ConcurrentDictionary<EscapedUTF8String, RpcMethodEntry> RpcMethods { get; set; } = new ConcurrentDictionary<EscapedUTF8String, RpcMethodEntry>();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public ReadOnlySpan<byte> Resolve(ArraySegment<byte> json,IJsonFormatterResolver formatterResolver)
        {
            var reader = new JsonReader(json.Array!, json.Offset);
            if (reader.ReadIsBeginObject())
            {

                bool versioned = false;
                EscapedUTF8String? methodName = null;
                ID? id = null;
                ArraySegment<byte> paramsSegment = default;
                try
                {
                    while (true)
                    {
                        reader.SkipWhiteSpace();
                        var buffer = reader.GetBufferUnsafe().AsSpan(reader.GetCurrentOffsetUnsafe());
                        ref var bufferRef = ref MemoryMarshal.GetReference(buffer);
                        const uint _id = (('"') | ('i' << 8) | ('d' << 16) | ('"' << 24));
                        const ulong method = ((ulong)'"') | ((ulong)'m' << 8) | ((ulong)'e' << 16) | ((ulong)'t' << 24) | ((ulong)'h' << 32) | ((ulong)'o' << 40) | ((ulong)'d' << 48) | ((ulong)'"' << 56);
                        const ulong @params = ((ulong)'"') | ((ulong)'p' << 8) | ((ulong)'a' << 16) | ((ulong)'r' << 24) | ((ulong)'a' << 32) | ((ulong)'m' << 40) | ((ulong)'s' << 48) | ((ulong)'"' << 56);
                        const ulong jsonrpc = (((ulong)'"') | ((ulong)'j' << 8) | ((ulong)'s' << 16) | ((ulong)'o' << 24) | ((ulong)'n' << 32) | ((ulong)'r' << 40) | ((ulong)'p' << 48) | ((ulong)'c' << 56));
                        const ulong ___2_0 = (((ulong)'"') | ((ulong)'2' << 8) | ((ulong)'.' << 16) | ((ulong)'0' << 24) | ((ulong)'"' << 32)) << 24;

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
                            case 11:
                                {
                                    if (Unsafe.ReadUnaligned<uint>(ref bufferRef) == _id)
                                    {
                                        goto ID;
                                    }
                                    else
                                    {
                                        goto BuildMessageFailed;
                                    }
                                }
                            //その次に短いのは"params":[]}または"params":{}}または"method":""}
                            case 12:
                            case 13:
                            case 14:
                            case 15:
                                {
                                    var chars8 = Unsafe.ReadUnaligned<ulong>(ref bufferRef);
                                    switch (chars8)
                                    {
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
                                                if ((uint)chars8 == _id)
                                                {
                                                    goto ID;

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
                                                if ((uint)chars8 == _id)
                                                {
                                                    goto ID;

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
                                        id = ID.Formatter.DeserializeSafe(ref reader);
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
                if (versioned && methodName is EscapedUTF8String name)
                {
                    if (RpcMethods.TryGetValue(name, out var entry))
                    {
                        return entry.ResolveRequest(paramsSegment, id, formatterResolver);
                    }
                    else
                    {
                        if (id is ID reqID)
                            return JsonSerializer.SerializeUnsafe(ErrorResponse.MethodNotFound(reqID, name.ToString()));
                        else return default; ;
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
                        return JsonSerializer.SerializeUnsafe(ErrorResponse.InvalidRequest(json));
                    }
                }
                catch (JsonParsingException)
                {

                }
                return JsonSerializer.SerializeUnsafe(ErrorResponse.ParseError(json));
            }

        }
        public ReadOnlySpan<byte> Resolve(ArraySegment<byte> json) => Resolve(json, JsonSerializer.DefaultResolver);

        public ReadOnlySpan<byte> Resolve(string json,IJsonFormatterResolver formatterResolver)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(json.Length << 2);
            var length = Encoding.UTF8.GetBytes(json, buffer);
            var segment = new ArraySegment<byte>(buffer, 0, length);
            var result = Resolve(segment,formatterResolver);
            ArrayPool<byte>.Shared.Return(buffer);
            return result;
        }
        public ReadOnlySpan<byte> Resolve(string json) => Resolve(json, JsonSerializer.DefaultResolver);
        public bool TryRegister(string name, RpcMethodEntry rpcMethod)
        {
            var methodName = EscapedUTF8String.FromUnEscaped(name);
            return RpcMethods.TryAdd(methodName, rpcMethod);
        }

        
        public bool TryUnregister(string name,[NotNullWhen(true)] out RpcMethodEntry? unregistered)
        {
            var methodName = EscapedUTF8String.FromUnEscaped(name);
            return RpcMethods.TryRemove(methodName,out unregistered);
        }


    }
}
