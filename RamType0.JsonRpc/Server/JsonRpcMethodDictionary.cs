using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;

namespace RamType0.JsonRpc.Server
{
    public class JsonRpcMethodDictionary : IDisposable
    {
        Dictionary<EscapedUTF8String, RpcEntry> RpcMethods { get; set; } = new Dictionary<EscapedUTF8String, RpcEntry>();
        public ValueTask InvokeAsync(IResponseOutput output,EscapedUTF8String methodName,ID? id,ref JsonReader reader,IJsonFormatterResolver formatterResolver)
        {
            if(RpcMethods.TryGetValue(methodName,out var invoker))
            {
                return invoker.ReadParamsAndInvokeAsync(this,output, id, ref reader, formatterResolver);
            }
            else
            {
                if (id is ID reqID)
                {
                    return output.ResponseError(ErrorResponse.MethodNotFound(reqID, methodName.ToString()));
                }
                else
                {
                    return new ValueTask();
                    
                }
            }
        }
        public void Invoke(IResponseOutput output, EscapedUTF8String methodName, ID? id, ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (RpcMethods.TryGetValue(methodName, out var invoker))
            {
                invoker.ReadParamsAndInvoke(this, output, id, ref reader, formatterResolver);
                return;
            }
            else
            {
                if (id is ID reqID)
                {
                    output.ResponseError(ErrorResponse.MethodNotFound(reqID, methodName.ToString()));
                }
                else
                {
                    return;
                }
            }
        }
        public void Register(string methodName,RpcEntry entry)
        {
            var rpcMethods = RpcMethods;
            //lock (rpcMethods)
            {
                rpcMethods.Add(EscapedUTF8String.FromUnEscaped(methodName), entry);
            }
        }
        internal CancellationTokenSource GetCancellationTokenSource(ID id)
        {
            var source = CancellationSourcePool.Get();
            if(!Cancellables.TryAdd(id, source))
            {
                throw new ArgumentException("ID conflicted!");//InvalidOperationExceptionにしてたけど同名の型作ったときはArgumentExceptionだったので・・・
            }
            return source;
        }
        internal DefaultObjectPool<CancellationTokenSource> CancellationSourcePool { get; } = new DefaultObjectPool<CancellationTokenSource>(CancellationSourcePooledPolicy.Instance);
        sealed class CancellationSourcePooledPolicy : PooledObjectPolicy<CancellationTokenSource>
        {
            public static CancellationSourcePooledPolicy Instance { get; } = new CancellationSourcePooledPolicy();
            public override CancellationTokenSource Create()
            {
                return new CancellationTokenSource();
            }

            public override bool Return(CancellationTokenSource obj)
            {
                if (obj.IsCancellationRequested)
                {
                    obj.Dispose();
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        internal ConcurrentDictionary<ID, CancellationTokenSource> Cancellables { get; } = new ConcurrentDictionary<ID, CancellationTokenSource>();
        

        public void Dispose()
        {
            foreach (var entry in RpcMethods.Values)
            {
                entry.Dispose();
            }
            RpcMethods = null!;

        }
 
    }
    public abstract class RpcEntry : IDisposable
    {
        public static RpcEntry FromDelegate<T>(T d)
            where T : Delegate
        {
            return Emit.FromDelegate(d).NewEntry(d);
        }
        public abstract void ReleasePooledClosures();
        public abstract ValueTask ReadParamsAndInvokeAsync(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, ID? id, ref JsonReader reader, IJsonFormatterResolver formatterResolver);
        public abstract void ReadParamsAndInvoke(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, ID? id, ref JsonReader reader, IJsonFormatterResolver formatterResolver);
        public abstract void Dispose();
    }
    public sealed class RpcEntry<TProxy, TDelegate, TParams, TDeserializer> : RpcEntry
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
        }
        public TProxy Proxy { get; private set; }
        public TDelegate RpcMethod { get; private set; }
        public TDeserializer ParamsDeserializer { get; private set; }

        bool ParamsIsEmpty { get; }

        public override void ReadParamsAndInvoke(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, ID? id, ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            reader.ReadIsBeginObjectWithVerify();
            ReadOnlySpan<byte> paramsStr = stackalloc byte[] { (byte)'p', (byte)'a', (byte)'r', (byte)'a', (byte)'m', (byte)'s', };
            TParams parameters;
            while (true)
            {

                JsonToken token = reader.GetCurrentJsonToken();
                switch (token)
                {
                    case JsonToken.String:
                        //IsProperty
                        {
                            //IsParams
                            if (reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(paramsStr))
                            {

                                var copyReader = reader;
                                try
                                {
                                    parameters = ParamsDeserializer.Deserialize(ref reader, formatterResolver);
                                }
                                catch (JsonParsingException)
                                {
                                    if (id is ID reqID)
                                    {
                                        var paramsJson = Encoding.UTF8.GetString(copyReader.ReadNextBlockSegment());//このメソッドが呼ばれた時点でParseErrorはありえない
                                        output.ResponseError(ErrorResponse.InvalidParams(reqID, paramsJson));
                                        return;
                                    }
                                    else
                                    {
                                        return;
                                    }

                                }
                                goto Invoke;
                            }
                            else
                            {
                                reader.ReadNextBlock();
                                if (!reader.ReadIsValueSeparator())
                                {
                                    reader.ReadIsEndObjectWithVerify();
                                    goto case JsonToken.EndObject;
                                }
                                continue;
                            }
                        }
                    case JsonToken.EndObject:
                        {
                            if (ParamsIsEmpty)
                            {
                                parameters = default!;
                                goto Invoke;
                            }
                            else
                            {
                                if (id is ID reqID)
                                {
                                    output.ResponseError(ErrorResponse.InvalidParams(reqID, "(not exists)"));
                                    return;
                                }
                                else
                                {
                                    return;
                                }
                            }
                        }

                    default:
                        {
                            throw new JsonParsingException($"Expected property or end of object,but {((char)reader.GetBufferUnsafe()[reader.GetCurrentOffsetUnsafe()]).ToString()}");
                        }
                }

            }
        Invoke:
            //paramsが読み取れた
            RpcMethodClosure<TProxy, TDelegate, TParams>.Invoke(methodDictionary, output, Proxy, RpcMethod, parameters, id);
            return;
        }

        public override void ReleasePooledClosures()
        {
            RpcMethodClosure<TProxy, TDelegate, TParams>.ReleasePooledClosures();
        }
        ~RpcEntry()
        {
            DisposeCore();
        }
        public override ValueTask ReadParamsAndInvokeAsync(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, ID? id, ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            reader.ReadIsBeginObjectWithVerify();
            ReadOnlySpan<byte> paramsStr = stackalloc byte[] { (byte)'p', (byte)'a', (byte)'r', (byte)'a', (byte)'m', (byte)'s', };
            TParams parameters;
            while (true)
            {

                JsonToken token = reader.GetCurrentJsonToken();
                switch (token)
                {
                    case JsonToken.String:
                        //IsProperty
                        {
                            //IsParams
                            if (reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(paramsStr))
                            {

                                var copyReader = reader;
                                try
                                {
                                    parameters = ParamsDeserializer.Deserialize(ref reader, formatterResolver);
                                }
                                catch (JsonParsingException)
                                {
                                    if (id is ID reqID)
                                    {
                                        var paramsJson = Encoding.UTF8.GetString(copyReader.ReadNextBlockSegment());//このメソッドが呼ばれた時点でParseErrorはありえない
                                        return output.ResponseError(ErrorResponse.InvalidParams(reqID, paramsJson));
                                    }
                                    else
                                    {
                                        return new ValueTask();
                                    }

                                }
                                goto Invoke;
                            }
                            else
                            {
                                reader.ReadNextBlock();
                                reader.ReadIsValueSeparatorWithVerify();
                                continue;
                            }
                        }
                    case JsonToken.EndObject:
                        {
                            if (ParamsIsEmpty)
                            {
                                parameters = default!;
                                goto Invoke;
                            }
                            else
                            {
                                if (id is ID reqID)
                                {
                                    return output.ResponseError(ErrorResponse.InvalidParams(reqID, "(not exists)"));
                                }
                                else
                                {
                                    return new ValueTask();
                                }
                            }
                        }

                    default:
                        {
                            throw new JsonParsingException($"Expected property or end of object,but {((char)reader.GetBufferUnsafe()[reader.GetCurrentOffsetUnsafe()]).ToString()}");
                        }
                }

            }
        Invoke:
            //paramsが読み取れた
            return RpcMethodClosure<TProxy, TDelegate, TParams>.Invoke(methodDictionary, output, Proxy, RpcMethod, parameters, id);
            //var closure = RpcMethodClosure<TProxy, TDelegate, TParams>.GetClosure(methodDictionary, output, Proxy, RpcMethod, parameters, id);
            //return new ValueTask(Task.Run(closure.InvokeAction));

        }
        public override void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }

        private void DisposeCore()
        {
            ReleasePooledClosures();
            Proxy = default!;
            RpcMethod = default!;
            ParamsDeserializer = default!;
        }
    }


}
