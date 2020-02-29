using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Server
{
    public class Server
    {
        internal ConcurrentDictionary<EscapedUTF8String, RpcMethodEntry> RpcMethods { get; set; } = new ConcurrentDictionary<EscapedUTF8String, RpcMethodEntry>();
        public IResponseOutput Output { get; }

        public IJsonFormatterResolver JsonResolver { get; }

        public IRpcExceptionHandler ExceptionHandler { get; }
        public Server(IResponseOutput output, IJsonFormatterResolver jsonResolver, IRpcExceptionHandler exceptionHandler)
        {
            Output = output;
            JsonResolver = jsonResolver;
            ExceptionHandler = exceptionHandler;
        }
        public Server (IResponseOutput output) : this(output, JsonSerializer.DefaultResolver) { }
        public Server(IResponseOutput output, IJsonFormatterResolver jsonResolver) : this(output, jsonResolver, DefaultRpcExceptionHandler.Instance) { }
        public bool Register(string methodName, RpcMethodEntry entry)
        {
            return RpcMethods.TryAdd(EscapedUTF8String.FromUnEscaped(methodName), entry);
        }

        public bool UnRegister(string methodName)
        {
            return RpcMethods.TryRemove(EscapedUTF8String.FromUnEscaped(methodName), out _);
        }

        
        public ValueTask ResolveAsync(ArraySegment<byte> json)
        {
            var reader = new JsonReader(json.Array!, json.Offset);
            if (reader.ReadIsBeginObject())
            {

                ReadOnlySpan<byte> jsonrpcSpan = stackalloc byte[] { (byte)'j', (byte)'s', (byte)'o', (byte)'n', (byte)'r', (byte)'p', (byte)'c', },
                    versionSpan = stackalloc byte[] { (byte)'2', (byte)'.', (byte)'0', },
                    methodSpan = stackalloc byte[] { (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d', },
                    idSpan = stackalloc byte[] { (byte)'i', (byte)'d', },
                paramsSpan = stackalloc byte[] { (byte)'p', (byte)'a', (byte)'r', (byte)'a', (byte)'m', (byte)'s', };
                bool versioned = false;
                EscapedUTF8String? methodName = null;
                ID? id = null;
                ArraySegment<byte> paramsSegment = default;
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
                                            goto BuildRequesetFailed;
                                        }
                                    }
                                    else if (propertySegmentSpan.SequenceEqual(methodSpan))
                                    {
                                        methodName = EscapedUTF8String.FromEscapedNonQuoted(reader.ReadStringSegmentRaw());
                                    }
                                    else if (propertySegmentSpan.SequenceEqual(idSpan))
                                    {
                                        id = ID.Formatter.DeserializeSafe(ref reader);
                                    }
                                    else if (propertySegmentSpan.SequenceEqual(paramsSpan))
                                    {
                                        paramsSegment = reader.ReadNextBlockSegment();
                                    }
                                    else
                                    {
                                        reader.ReadNextBlock();
                                    }

                                    if (reader.ReadIsEndObject())
                                    {
                                        goto ReachedObjectTerminal;
                                    }
                                    else
                                    {
                                        if (!reader.ReadIsValueSeparator())
                                        {
                                            goto BuildRequesetFailed;
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
                                    goto BuildRequesetFailed;
                                }
                        }
                    }
                    catch (JsonParsingException)
                    {
                        goto BuildRequesetFailed;
                    }
                }
            ReachedObjectTerminal:
                if (versioned && methodName is EscapedUTF8String name)
                {
                    if (RpcMethods.TryGetValue(name, out var entry))
                    {
                        return entry.InvokeAsync(this, paramsSegment, id);
                    }
                    else
                    {
                        if (id is ID reqID)
                            return Output.ResponseError(this,ErrorResponse.MethodNotFound(reqID, name.ToString()));
                        else return new ValueTask();
                    }
                }
                else
                {
                    goto BuildRequesetFailed;
                }
            }
            else
            {
                goto BuildRequesetFailed;
            }

        BuildRequesetFailed:
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
                        return Output.ResponseError(this,ErrorResponse.InvalidRequest(json));
                    }
                }
                catch (JsonParsingException)
                {

                }
                return Output.ResponseError(this,ErrorResponse.ParseError(json));
            }

        }
        public ValueTask ResolveAsync(string json)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(json.Length * 4);
            var segment = new ArraySegment<byte>(buffer, 0, Encoding.UTF8.GetBytes(json, buffer));
            var ret = ResolveAsync(segment);
            ArrayPool<byte>.Shared.Return(segment.Array!);
            return ret;
        }
    }
    public abstract class RpcMethodEntry
    {
        public static RpcMethodEntry FromDelegate<T>(T d)
            where T : Delegate
        {
            return Emit.FromDelegate(d).NewEntry(d);
        }
        public abstract ValueTask InvokeAsync(Server server, ArraySegment<byte> parametersSegment, ID? id);
    }
    public sealed class RpcEntry<TProxy, TDelegate, TParams, TDeserializer> : RpcMethodEntry
        where TProxy : notnull, IRpcMethodProxy<TDelegate, TParams>
        where TDelegate : Delegate
        where TParams : IMethodParams
        where TDeserializer : notnull, IParamsDeserializer<TParams>
    {
        public RpcEntry(TProxy proxy, TDelegate rpcMethod, TDeserializer paramsDeserializer)
        {
            Proxy = proxy;
            RpcMethod = rpcMethod;
            ParamsDeserializer = paramsDeserializer;
            ParamsIsEmpty = typeof(IEmptyParams).IsAssignableFrom(typeof(TParams));
            if (ParamsIsEmpty && default(TParams)! == null)
            {
                DefaultParams = (TParams)Activator.CreateInstance(typeof(TParams))!;

            }

        }
        public TProxy Proxy { get; private set; }
        public TDelegate RpcMethod { get; private set; }
        public TDeserializer ParamsDeserializer { get; private set; }

        bool ParamsIsEmpty { get; }
        TParams DefaultParams { get; } = default!;
        public override ValueTask InvokeAsync(Server server, ArraySegment<byte> parametersSegment, ID? id)
        {

            TParams parameters;
            if (parametersSegment.Count > 0)
            {
                var reader = new JsonReader(parametersSegment.Array!, parametersSegment.Offset);

                try
                {
                    parameters = ParamsDeserializer.Deserialize(ref reader, server.JsonResolver);
                }
                catch (JsonParsingException)
                {
                    if (id is ID reqID)
                    {
                        return server.Output.ResponseError(server,ErrorResponse.InvalidParams(reqID, Encoding.UTF8.GetString(parametersSegment)));
                    }
                    else
                    {
                        return new ValueTask();
                    }

                }
            }
            else
            {
                if (ParamsIsEmpty)
                {
                    parameters = DefaultParams;
                }
                else
                {
                    if (id is ID reqID)
                    {
                        return server.Output.ResponseError(server,ErrorResponse.InvalidParams(reqID, "<missing>"));
                    }
                    else
                    {
                        return new ValueTask();
                    }
                }
            }

            return Proxy.DelegateResponse(server, RpcMethod, parameters, id);

        }
    }
}
