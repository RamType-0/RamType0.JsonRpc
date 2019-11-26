using System;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Server
{
    public struct DefaultObjectStyleParamsDeserializer<T> : IObjectStyleParamsDeserializer<T>
            where T : struct, IMethodParams
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);
        }
    }
    public interface IParamsDeserializer<T>
        where T : IMethodParams
    {
        T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
    }
    public readonly struct ParamsDeserializer<T, TObjectStyle, TArrayStyle> : IParamsDeserializer<T>
        where T : struct, IMethodParams
        where TObjectStyle : struct, IObjectStyleParamsDeserializer<T>
        where TArrayStyle : struct, IArrayStyleParamsDeserializer<T>
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            T paramsObj;
            switch (reader.GetCurrentJsonToken())
            {
                case JsonToken.BeginObject:
                    {
                        paramsObj = default(TObjectStyle).Deserialize(ref reader, formatterResolver);
                        break;
                    }
                case JsonToken.BeginArray:
                    {
                        paramsObj = default(TArrayStyle).Deserialize(ref reader, formatterResolver);
                        break;
                    }
                default:
                    {
                        throw new JsonParsingException("ParamsObject was not array, neither object.");
                    }
            }
            return paramsObj;
        }



    }
    public interface IArrayStyleParamsDeserializer<T> : IParamsDeserializer<T>
        where T : struct, IMethodParams
    {


    }
    public interface IObjectStyleParamsDeserializer<T> : IParamsDeserializer<T>
        where T : struct, IMethodParams
    {

    }
    /// <summary>
    /// <typeparamref name="TParams"/>を<typeparamref name="TDelegate"/>の引数に変換し、呼び出しを行います。
    /// </summary>
    /// <typeparam name="TDelegate">呼び出し対象の<see langword="delegate"/>の具象型。</typeparam>
    /// <typeparam name="TParams"><typeparamref name="TDelegate"/>の引数へ変換される型。</typeparam>
    public interface IRpcDelegateInvoker<TDelegate, in TParams>
        where TDelegate : Delegate
        where TParams : IMethodParams
    {
        void Invoke(TDelegate invokedDelegate, TParams parameters);
    }
    public interface IRpcActionInvoker<TDelegate, in TParams> : IRpcDelegateInvoker<TDelegate, TParams>
        where TDelegate : Delegate
        where TParams : IMethodParams
    {

    }
    public interface IRpcFunctionInvoker<TDelegate, in TParams, out TResult> : IRpcDelegateInvoker<TDelegate, TParams>
        where TDelegate : Delegate
        where TParams : IMethodParams
    {
        new TResult Invoke(TDelegate invokedDelegate, TParams parameters);
        void IRpcDelegateInvoker<TDelegate, TParams>.Invoke(TDelegate invokedDelegate, TParams parameters) => Invoke(invokedDelegate, parameters);
    }
    public interface IRpcMethodProxy<TDelegate, in TParams>
        where TDelegate : Delegate
        where TParams : IMethodParams
    {
        /// <summary>
        /// <paramref name="rpcMethod"/>に<paramref name="parameters"/>を引数に変換して呼び出し、<paramref name="id"/>が<see langword="null"/>でなければ呼び出し結果に基づいたレスポンスを生成、それを<paramref name="output"/>に伝えるまでを全て代行します。
        /// </summary>
        /// <param name="methodDictionary">呼び出し元の<see cref="JsonRpcMethodDictionary"/>。</param>
        /// <param name="output">レスポンスの最終的な出力を行う<see cref="IResponseOutput"/>。</param>
        /// <param name="rpcMethod"></param>
        /// <param name="parameters"></param>
        /// <param name="id"></param>
        ValueTask DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, TDelegate rpcMethod, TParams parameters, ID? id = null);
    }

    public readonly struct DefaultFunctionProxy<TDelegate, TParams, TResult, TInvoker> : IRpcMethodProxy<TDelegate, TParams>
        where TInvoker : notnull, IRpcFunctionInvoker<TDelegate, TParams, TResult>
                    where TDelegate : Delegate
            where TParams : IMethodParams
    {
        public DefaultFunctionProxy(TInvoker invoker)
        {
            Invoker = invoker;
        }

        public TInvoker Invoker { get; }

        public ValueTask DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            TResult result;
            try
            {
                result = Invoker.Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                if (id is ID requestID)
                {
                    return output.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                }
                return new ValueTask();
            }
            {
                if (id is ID requestID)
                {
                    return output.ResponseResult(new ResultResponse<TResult>(requestID, result));
                }
                return new ValueTask();
            }
        }

    }
    public readonly struct DefaultActionProxy<TDelegate, TParams, TInvoker> : IRpcMethodProxy<TDelegate, TParams>
        where TInvoker : notnull, IRpcActionInvoker<TDelegate, TParams>
                    where TDelegate : Delegate
            where TParams : IMethodParams
    {
        public DefaultActionProxy(TInvoker invoker)
        {
            Invoker = invoker;
        }

        public TInvoker Invoker { get; }
        public ValueTask DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            try
            {
                Invoker.Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                if (id is ID requestID)
                {
                    return output.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                }
                return new ValueTask();
            }
            {
                if (id is ID requestID)
                {
                    return output.ResponseResult(ResultResponse.Create(requestID));
                }
                return new ValueTask();
            }
        }

    }

    public readonly struct DefaultCancellableActionProxy<TDelegate, TParams, TInvoker> : IRpcMethodProxy<TDelegate, TParams>
        where TInvoker : notnull, IRpcActionInvoker<TDelegate, TParams>
                    where TDelegate : Delegate
            where TParams : ICancellableMethodParams
    {
        public DefaultCancellableActionProxy(TInvoker invoker)
        {
            Invoker = invoker;
        }

        public TInvoker Invoker { get; }
        public ValueTask DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            try
            {

                try
                {
                    if (id is ID reqID)
                    {
                        var cancellationTokenSource = methodDictionary.GetCancellationTokenSource(reqID);
                        parameters.CancellationToken = cancellationTokenSource.Token;

                    }

                    Invoker.Invoke(rpcMethod, parameters);
                }
                catch (Exception e)
                {
                    if (id is ID requestID)
                    {
                        if (e is OperationCanceledException)
                        {
                            return output.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));//TODO:キャンセルの別途ハンドリング
                        }
                        else
                        {
                            return output.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                        }
                    }
                    return new ValueTask();
                }
                {
                    if (id is ID requestID)
                    {
                        return output.ResponseResult(ResultResponse.Create(requestID));
                    }
                    return new ValueTask();
                }
            }
            finally
            {
                if (id is ID reqID && methodDictionary.Cancellables.TryRemove(reqID, out var cancellationTokenSource))
                {

                    methodDictionary.CancellationSourcePool.Return(cancellationTokenSource);
                }
            }
            
        }

    }

    public readonly struct DefaultCancellableFunctionProxy<TDelegate, TParams, TResult, TInvoker> : IRpcMethodProxy<TDelegate, TParams>
      where TInvoker : notnull, IRpcFunctionInvoker<TDelegate, TParams, TResult>
                  where TDelegate : Delegate
          where TParams : ICancellableMethodParams
    {
        public DefaultCancellableFunctionProxy(TInvoker invoker)
        {
            Invoker = invoker;
        }

        public TInvoker Invoker { get; }
        public ValueTask DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            try
            {
                TResult result;

                try
                {
                    if (id is ID reqID)
                    {
                        var cancellationTokenSource = methodDictionary.GetCancellationTokenSource(reqID);
                        parameters.CancellationToken = cancellationTokenSource.Token;

                    }

                    result = Invoker.Invoke(rpcMethod, parameters);
                }
                catch (Exception e)
                {
                    if (id is ID requestID)
                    {
                        if (e is OperationCanceledException)
                        {
                            return output.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));//TODO:キャンセルの別途ハンドリング
                        }
                        else
                        {
                            return output.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                        }
                    }
                    return new ValueTask();
                }
                {
                    if (id is ID requestID)
                    {
                        return output.ResponseResult(ResultResponse.Create(requestID, result));
                    }
                    return new ValueTask();
                }
            }
            finally
            {
                if (id is ID reqID && methodDictionary.Cancellables.TryRemove(reqID, out var cancellationTokenSource))
                {

                    methodDictionary.CancellationSourcePool.Return(cancellationTokenSource);
                }
            }
        }

    }
}
