using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Client
{
    using Protocol;
    public class Client
    {
        public bool TryResolveResponse(ArraySegment<byte> json)
        {
            var reader = new JsonReader(json.Array!, json.Offset);
            if (reader.ReadIsBeginObject())
            {

                bool versioned = false;
                //EscapedUTF8String? methodName = null;
                ID? _id = null;
                //ArraySegment<byte> paramsSegment = default;
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
                        //const ulong method = ((ulong)'"') | ((ulong)'m' << 8) | ((ulong)'e' << 16) | ((ulong)'t' << 24) | ((ulong)'h' << 32) | ((ulong)'o' << 40) | ((ulong)'d' << 48) | ((ulong)'"' << 56);
                        //const ulong @params = ((ulong)'"') | ((ulong)'p' << 8) | ((ulong)'a' << 16) | ((ulong)'r' << 24) | ((ulong)'a' << 32) | ((ulong)'m' << 40) | ((ulong)'s' << 48) | ((ulong)'"' << 56);
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
                                        _error = JsonResolver.GetFormatterWithVerify<ResponseError<object?>>().Deserialize(ref reader, JsonResolver);
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

                {
                    if (_id is ID id)
                    {
                        if (UnResponsedRequests.TryRemove(id, out var req))
                        {
                            try
                            {
                                if (_error is ResponseError<object?> error)
                                {
                                    req.SetException(ErrorHandler.AsException(error));
                                }
                                else
                                {
                                    if(versioned)
                                    req.SetResult(resultSegment, JsonResolver);
                                    else
                                    {
                                        req.SetException(new FormatException("JsonRpc server returned non versioned response."));
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                req.SetException(e);
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (_error is ResponseError<object?> error)
                        {
                            UnIdentifiableErrors.Add(error);
                            return false;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                
            }
            else
            {
                goto BuildMessageFailed;
            }
        BuildMessageFailed:
            return false;
        }

        internal ConcurrentDictionary<ID, RequestCompletionSource> UnResponsedRequests { get; } = new ConcurrentDictionary<ID, RequestCompletionSource>();
        public ConcurrentBag<ResponseError<object?>> UnIdentifiableErrors { get; } = new ConcurrentBag<ResponseError<object?>>();
        public IJsonFormatterResolver JsonResolver { get; }
        public IRequestOutput Output { get; }
        public IResponseErrorHandler ErrorHandler { get; }
        long idSource = 0;

        public Client(IRequestOutput output, IJsonFormatterResolver jsonResolver,  IResponseErrorHandler errorHandler)
        {
            JsonResolver = jsonResolver;
            Output = output;
            ErrorHandler = errorHandler;
        }

        public Client(IRequestOutput output, IJsonFormatterResolver jsonResolver):this(output,jsonResolver,DefaultResponseErrorHandler.Instance) { }

        internal ID GetUniqueID()
        {
            return new ID(Interlocked.Increment(ref idSource));//1秒に40億件処理してもマイナス値になるまで60年以上かかるので問題ないはず・・・
        }

    }
    public abstract class RequestObjectSource
    {
        public Client Client { get; }
        public string MethodName { get; }
        EscapedUTF8String EscapedMethodName { get; }
        public RequestObjectSource(Client client, string name)
        {
            Client = client;
            MethodName = name;
            EscapedMethodName = EscapedUTF8String.FromUnEscaped(name);
        }
        protected async ValueTask<TResult> RequestAsync<TParams, TResult>(TParams parameters)
            where TParams : IMethodParams

        {
            var request = new Request<TParams>() { ID = Client.GetUniqueID(), Method = EscapedMethodName, Params = parameters };
            var completion = RequestCompletionSource<TResult>.Get();
            Client.UnResponsedRequests.TryAdd(request.ID, completion);
            await Client.Output.SendRequestAsync(Client, request).ConfigureAwait(false);
            return await completion.ValueTask.ConfigureAwait(false);

        }
        protected async ValueTask RequestAsync<TParams>(TParams parameters)
            where TParams : IMethodParams

        {
            var request = new Request<TParams>() { ID = Client.GetUniqueID(), Method = EscapedMethodName, Params = parameters };
            var completion = RequestCompletionSource<NullResult>.Get();
            Client.UnResponsedRequests.TryAdd(request.ID, completion);
            await Client.Output.SendRequestAsync(Client, request).ConfigureAwait(false);
            await completion.VoidValueTask.ConfigureAwait(false);

        }
        protected TResult Request<TParams, TResult>(TParams parameters)
            where TParams : IMethodParams
        {
            return RequestAsync<TParams, TResult>(parameters).Result;
        }
        protected void Request<TParams>(TParams parameters)
            where TParams : IMethodParams
        {
            RequestAsync<TParams>(parameters).GetAwaiter().GetResult();//WAIT!!!!
        }
        protected ValueTask Notify<TParams>(TParams parameters)
            where TParams : IMethodParams
        {
            var notification = new Notification<TParams>() { Method = EscapedMethodName, Params = parameters };
            return Client.Output.SendNotification(Client, notification);
        }
    }
    public sealed class Requester<TParams, TResult> : RequestObjectSource
        where TParams : IMethodParams
    {
        public Requester(Client client, string name) : base(client, name)
        {
        }
        public ValueTask<TResult> RequestAsync(TParams parameters)
        {
            return RequestAsync<TParams, TResult>(parameters);
        }
        public TResult Request(TParams parameters)
        {
            return Request<TParams, TResult>(parameters);
        }
    }
    public sealed class Requester<TParams> : RequestObjectSource
        where TParams : IMethodParams
    {
        public Requester(Client client, string name) : base(client, name)
        {
        }
        public ValueTask RequestAsync(TParams parameters)
        {
            return RequestAsync<TParams>(parameters);
        }
        public void Request(TParams parameters)
        {
            Request<TParams>(parameters);
        }
    }

    public sealed class Notifier<TParams> : RequestObjectSource
        where TParams : IMethodParams
    {
        public Notifier(Client client, string name) : base(client, name)
        {
        }
        public void Notify(TParams parameters)
        {
            base.Notify(parameters);
        }
    }

}
