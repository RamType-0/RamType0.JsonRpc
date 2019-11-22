using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;
using System.Reflection.Emit;
using System.Reflection;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;
using static RamType0.JsonRpc.Emit;

namespace RamType0.JsonRpc
{
    public class JsonRpcMethodDictionary
    {
        Dictionary<EscapedUTF8String, RpcInvoker> RpcMethods { get; } = new Dictionary<EscapedUTF8String, RpcInvoker>();

        //delegate Task RpcInvokerMethod(IResponser responser, ref JsonReader reader, ID? id, IJsonFormatterResolver formatterResolver);
       
        public ValueTask InvokeAsync(IResponser responser,EscapedUTF8String methodName,ID? id,ref JsonReader reader,IJsonFormatterResolver formatterResolver)
        {
            if(RpcMethods.TryGetValue(methodName,out var invoker))
            {
                return invoker.ReadParamsAndInvokeAsync(this,responser, ref reader, id, formatterResolver);
            }
            else
            {
                if (id is ID reqID)
                {
                    return new ValueTask(Task.Run(() => responser.ResponseError(ErrorResponse.MethodNotFound(reqID, methodName.ToString()))));
                }
                else
                {
                    return new ValueTask();
                    
                }
            }
        }

        public void Invoke(IResponser responser, EscapedUTF8String methodName, ID? id, ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            if (RpcMethods.TryGetValue(methodName, out var invoker))
            {
                invoker.ReadParamsAndInvoke(this, responser, ref reader, id, formatterResolver);
                return;
            }
            else
            {
                if (id is ID reqID)
                {
                    responser.ResponseError(ErrorResponse.MethodNotFound(reqID, methodName.ToString()));
                }
                else
                {
                    return;
                }
            }
        }

        public void Register<T>(string methodName,T method)
            where T:Delegate
        {
            var paramsType = MethodParamsTypeBuilder.CreateParamsType(method, methodName);
            var invoker = Unsafe.As<RpcInvoker>(Activator.CreateInstance(RpcInvoker.GetDefaultInvokerType(paramsType)));
            //var invoker = Unsafe.As<RpcInvokerMethod>(typeof(RpcInvoker).GetMethod("ReadParamsAndInvokeAsync")!.MakeGenericMethod(paramsType).CreateDelegate(typeof(RpcInvokerMethod)));
            RpcMethods.Add(EscapedUTF8String.FromUnEscaped(methodName), invoker);
        }

        internal CancellationTokenSource GetCancellationTokenSource(ID id)
        {
            var source = CancellationSourcePool.Get();
            if(!Cancellables.TryAdd(id, source))
            {
                throw new InvalidOperationException("ID conflicted!");
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
        internal abstract class RpcInvoker:IDisposable
        {
            public abstract void ReleasePooledClosures();
            
            public abstract ValueTask ReadParamsAndInvokeAsync(JsonRpcMethodDictionary methodDictionary,IResponser responser, ref JsonReader reader, ID? id, IJsonFormatterResolver formatterResolver);
            public abstract void ReadParamsAndInvoke(JsonRpcMethodDictionary methodDictionary, IResponser responser, ref JsonReader reader, ID? id, IJsonFormatterResolver formatterResolver);


            public abstract void Dispose();

            public static Type GetDefaultInvokerType(Type paramsType)
            {
                Type proxyType;
                if(typeof(IActionParamsObject).IsAssignableFrom(paramsType))
                {
                    if (typeof(ICancellableMethodParamsObject).IsAssignableFrom(paramsType))
                    {
                        proxyType = typeof(CancellableRpcActionProxy<>).MakeGenericType(paramsType);
                    }
                    else
                    {


                        proxyType = typeof(DefaultRpcActionProxy<>).MakeGenericType(paramsType);
                    }
                }
                else
                {
                    foreach (var interfaceType in paramsType.GetInterfaces())
                    {
                        if(interfaceType.IsConstructedGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IFunctionParamsObject<>))
                        {
                            var resultType = interfaceType.GetGenericArguments()[0];
                            if (typeof(ICancellableMethodParamsObject).IsAssignableFrom(paramsType))
                            {
                                proxyType = typeof(CancellableRpcFunctionProxy<,>).MakeGenericType(paramsType,resultType);
                            }
                            else
                            {
                                proxyType = typeof(DefaultRpcFunctionProxy<,>).MakeGenericType(paramsType, resultType);
                            }
                            
                            goto ReturnInvokerType;
                        }
                    }
                    throw new ArgumentException();
                }
                ReturnInvokerType:
                return typeof(RpcInvoker<,>).MakeGenericType(paramsType, proxyType);
            }
        }

        sealed class RpcInvoker<TParams,TProxy> : RpcInvoker
             where TParams : struct, IMethodParamsObject
            where TProxy : struct,IRpcMethodInvokationProxy<TParams>
        {
            public override void ReleasePooledClosures()
            {
                RpcMethodClosure<TParams,TProxy>.ReleasePooledClosures();
            }

            public override void Dispose()
            {
                DisposeClosuresAndDelegate();
                GC.SuppressFinalize(this);
            }

            private void DisposeClosuresAndDelegate()
            {

                RpcMethodClosure<TParams, TProxy>.ReleasePooledClosures();
                default(TParams).Dispose();
            }

            ~RpcInvoker()
            {
                DisposeClosuresAndDelegate();
            }

            /// <summary>
            /// このメソッドの呼び出し後、readerの状態は未定義です。使用する場合、予めコピーしておいてください。
            /// </summary>
            /// <param name="reader">RequestObject全体を与えられた<see cref="JsonReader"/>。</param>
            /// <param name="id"></param>
            /// <param name="formatterResolver"></param>
            /// <returns></returns>
            public override ValueTask ReadParamsAndInvokeAsync(JsonRpcMethodDictionary methodDictionary,IResponser responser, ref JsonReader reader, ID? id, IJsonFormatterResolver formatterResolver)

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
                                        parameters = ParamsFormatter<TParams>.Instance.Deserialize(ref reader, formatterResolver);
                                    }
                                    catch (JsonParsingException)
                                    {
                                        if (id is ID reqID)
                                        {
                                            var paramsJson = Encoding.UTF8.GetString(copyReader.ReadNextBlockSegment());//このメソッドが呼ばれた時点でParseErrorはありえない
                                            return new ValueTask(Task.Run(() => responser.ResponseError(ErrorResponse.InvalidParams(reqID, paramsJson))));
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
                                if (default(TParams) is IEmptyParamsObject)
                                {
                                    parameters = default;
                                    goto Invoke;
                                }
                                else
                                {
                                    if (id is ID reqID)
                                    {
                                        return new ValueTask(Task.Run(() => responser.ResponseError(ErrorResponse.InvalidParams(reqID, "(not exists)"))));
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
                var closure = RpcMethodClosure<TParams,TProxy>.GetClosure(methodDictionary,responser, parameters, id);
                return new ValueTask(Task.Run(closure.InvokeAction));

            }

            public override void ReadParamsAndInvoke(JsonRpcMethodDictionary methodDictionary, IResponser responser, ref JsonReader reader, ID? id, IJsonFormatterResolver formatterResolver)

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
                                        parameters = ParamsFormatter<TParams>.Instance.Deserialize(ref reader, formatterResolver);
                                    }
                                    catch (JsonParsingException)
                                    {
                                        if (id is ID reqID)
                                        {
                                            var paramsJson = Encoding.UTF8.GetString(copyReader.ReadNextBlockSegment());//このメソッドが呼ばれた時点でParseErrorはありえない
                                            responser.ResponseError(ErrorResponse.InvalidParams(reqID, paramsJson));
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
                                    reader.ReadIsValueSeparatorWithVerify();
                                    continue;
                                }
                            }
                        case JsonToken.EndObject:
                            {
                                if (default(TParams) is IEmptyParamsObject)
                                {
                                    parameters = default;
                                    goto Invoke;
                                }
                                else
                                {
                                    if (id is ID reqID)
                                    {
                                        responser.ResponseError(ErrorResponse.InvalidParams(reqID, "(not exists)"));
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
                RpcMethodClosure<TParams, TProxy>.InvokeWithLogging(methodDictionary, responser, parameters, id);
                return;
            }


        }

        internal sealed class RpcInvoker<TProxy,TDelegate,TParams,TDeserializer> : RpcInvoker
            where TProxy:struct,RpcResponseCreater<TDelegate,TParams>
            where TDelegate : Delegate
            where TParams : struct,IMethodParams
            where TDeserializer :struct,IParamsDeserializer<TParams>
        {
            public RpcInvoker(TDelegate rpcMethod)
            {
                this.rpcMethod = rpcMethod;
            }
            public TDelegate rpcMethod;
            public override void ReadParamsAndInvoke(JsonRpcMethodDictionary methodDictionary, IResponser responser, ref JsonReader reader, ID? id, IJsonFormatterResolver formatterResolver)
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
                                        parameters = default(TDeserializer).Deserialize(ref reader, formatterResolver);
                                    }
                                    catch (JsonParsingException)
                                    {
                                        if (id is ID reqID)
                                        {
                                            var paramsJson = Encoding.UTF8.GetString(copyReader.ReadNextBlockSegment());//このメソッドが呼ばれた時点でParseErrorはありえない
                                            responser.ResponseError(ErrorResponse.InvalidParams(reqID, paramsJson));
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
                                    reader.ReadIsValueSeparatorWithVerify();
                                    continue;
                                }
                            }
                        case JsonToken.EndObject:
                            {
                                if (default(TParams) is IEmptyParams)
                                {
                                    parameters = default;
                                    goto Invoke;
                                }
                                else
                                {
                                    if (id is ID reqID)
                                    {
                                        responser.ResponseError(ErrorResponse.InvalidParams(reqID, "(not exists)"));
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
                RpcMethodClosure<TProxy,TDelegate,TParams>.InvokeWithLogging(methodDictionary, responser,rpcMethod, parameters, id);
                return;
            }

            public override void ReleasePooledClosures()
            {
                RpcMethodClosure<TProxy, TDelegate, TParams>.ReleasePooledClosures();
            }
            ~RpcInvoker()
            {
                ReleasePooledClosures();
            }
            public override ValueTask ReadParamsAndInvokeAsync(JsonRpcMethodDictionary methodDictionary, IResponser responser, ref JsonReader reader, ID? id, IJsonFormatterResolver formatterResolver)
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
                                        parameters = default(TDeserializer).Deserialize(ref reader, formatterResolver);
                                    }
                                    catch (JsonParsingException)
                                    {
                                        if (id is ID reqID)
                                        {
                                            var paramsJson = Encoding.UTF8.GetString(copyReader.ReadNextBlockSegment());//このメソッドが呼ばれた時点でParseErrorはありえない
                                            return new ValueTask(Task.Run(() => responser.ResponseError(ErrorResponse.InvalidParams(reqID, paramsJson))));
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
                                if (default(TParams) is IEmptyParams)
                                {
                                    parameters = default;
                                    goto Invoke;
                                }
                                else
                                {
                                    if (id is ID reqID)
                                    {
                                        return new ValueTask(Task.Run(() => responser.ResponseError(ErrorResponse.InvalidParams(reqID, "(not exists)"))));
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
                var closure = RpcMethodClosure<TProxy, TDelegate, TParams>.GetClosure(methodDictionary, responser,rpcMethod, parameters, id);
                return new ValueTask(Task.Run(closure.InvokeAction));

            }

            public override void Dispose()
            {
                ReleasePooledClosures();
                GC.SuppressFinalize(this);
            }
        }


        /// <summary>
        /// <typeparamref name="TParams"/>を<typeparamref name="TDelegate"/>の引数に変換し、呼び出しを行います。
        /// </summary>
        /// <typeparam name="TDelegate">呼び出し対象の<see langword="delegate"/>の具象型。</typeparam>
        /// <typeparam name="TParams"><typeparamref name="TDelegate"/>の引数へ変換される型。</typeparam>
        public interface IRpcDelegateInvoker<TDelegate,in TParams>
            where TDelegate :Delegate
            where TParams: IMethodParams
        {
            void Invoke(TDelegate invokedDelegate, TParams parameters);
        }
        public interface IRpcActionInvoker<TDelegate, in TParams> : IRpcDelegateInvoker<TDelegate,TParams>
            where TDelegate : Delegate
            where TParams : IMethodParams
        {

        }
        public interface IRpcFunctionInvoker<TDelegate, in TParams,out TResult> : IRpcDelegateInvoker<TDelegate,TParams>
            where TDelegate : Delegate
            where TParams : IMethodParams
        {
            new TResult Invoke(TDelegate invokedDelegate, TParams parameters);
            void IRpcDelegateInvoker<TDelegate, TParams>.Invoke(TDelegate invokedDelegate, TParams parameters) => Invoke(invokedDelegate, parameters);
        }


        public interface RpcResponseCreater<TDelegate, in TParams>
            where TDelegate:Delegate
            where TParams:IMethodParams
        {
            /// <summary>
            /// <paramref name="rpcMethod"/>に<paramref name="parameters"/>を引数に変換して呼び出し、<paramref name="id"/>が<see langword="null"/>でなければ呼び出し結果に基づいたレスポンスを生成、それを<paramref name="responser"/>に伝えるまでを全て代行します。
            /// </summary>
            /// <param name="methodDictionary">呼び出し元の<see cref="JsonRpcMethodDictionary"/>。</param>
            /// <param name="responser">レスポンスの最終的な処理を行う<see cref="IResponser"/>。</param>
            /// <param name="rpcMethod"></param>
            /// <param name="parameters"></param>
            /// <param name="id"></param>
            void DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponser responser,TDelegate rpcMethod , TParams parameters, ID? id = null);
        }
    }


}
