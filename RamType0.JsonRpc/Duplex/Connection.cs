using RamType0.JsonRpc.Client;
using RamType0.JsonRpc.Server;
using System;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Duplex
{
    /// <summary>
    /// 単一の入力でクライアント、サーバー双方の通信を取り持つクラスです。
    /// </summary>
    public sealed class Connection
    {
        public Connection(Server.Server server, Client.Client client)
        {
            Server = server;
            Client = client;
        }

        public Server.Server Server { get; }
        public Client.Client Client { get; }
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
                                        _error =  Client.JsonResolver.GetFormatterWithVerify<ResponseError<object?>>().Deserialize(ref reader, Client.JsonResolver);
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
                                    req.SetException(Client.ErrorHandler.AsException(error));
                                }
                                else
                                {
                                    req.SetResult(resultSegment,Client.JsonResolver);
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
    }
}
