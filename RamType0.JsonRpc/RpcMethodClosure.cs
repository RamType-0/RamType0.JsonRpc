using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using Utf8Json;

namespace RamType0.JsonRpc
{


    public sealed class RpcMethodClosure<TProxy, TDelegate, TParams>
         where TProxy : struct, JsonRpcMethodDictionary.RpcResponseCreater<TDelegate, TParams>
            where TDelegate : Delegate
            where TParams : struct, IMethodParams
    {
        public JsonRpcMethodDictionary RpcMethodDictionary { get; private set; } = default!;

        public IResponser Responser { get; private set; } = default!;
        public TDelegate RpcMethod { get; private set; } = default!;
        public TParams Params { get; private set; }
        public ID? ID { get; private set; }
        public void InvokeWithPoolingAndLogging()
        {
            try
            {
                InvokeWithLogging(RpcMethodDictionary, Responser,RpcMethod, Params, ID);
            }
            finally
            {
                Pool.Return(this);
            }
        }

        public static void InvokeWithLogging(JsonRpcMethodDictionary methodDictionary, IResponser responser,TDelegate rpcMethod, TParams parameters, ID? id)
        {
            try
            {
               
                default(TProxy).DelegateResponse(methodDictionary, responser,rpcMethod ,parameters, id);
            }
            catch (Exception e)
            {

                var errorOutput = Stream.Synchronized(Console.OpenStandardError());//ここあたりのメソッドまともに使ったこと無いのでこれでも危険かもしれない
                errorOutput.Write(ErrorLogHeader);
                var unHandledException = JsonSerializer.SerializeUnsafe(e);
                errorOutput.Write(unHandledException);
                throw;
            }
        }

        static byte[] ErrorLogHeader { get; } = Encoding.UTF8.GetBytes($"Unhandled exception from {typeof(TProxy).Name}. \n");
        internal RpcMethodClosure()
        {
            InvokeAction = InvokeWithPoolingAndLogging;
        }

        /// <summary>
        /// プーリングを行いつつ新たなクロージャを取得します。プーリング機構が破棄されている場合、プーリングは行われません。
        /// </summary>
        /// <param name="responser"></param>
        /// <param name="parameters"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static RpcMethodClosure<TProxy, TDelegate, TParams> GetClosure(JsonRpcMethodDictionary methodDictionary, IResponser responser,TDelegate rpcMethod, TParams parameters, ID? id)
        {

            var closure = Pool.Get();
            closure.Inject(methodDictionary, responser,rpcMethod, parameters, id);
            return closure;
        }



        

        void Inject(JsonRpcMethodDictionary methodDictionary, IResponser responser,TDelegate rpcMethod ,TParams parameters, ID? id)
        {
            RpcMethodDictionary = methodDictionary;
            Responser = responser;
            RpcMethod = rpcMethod;
            Params = parameters;
            ID = id;
        }



        public Action InvokeAction { get; }
        public static void ReleasePooledClosures()
        {

            Pool = new DefaultObjectPool<RpcMethodClosure<TProxy, TDelegate, TParams>>(PooledPolicy.Instance);


        }
        #region Pooling
        sealed class PooledPolicy : PooledObjectPolicy<RpcMethodClosure<TProxy,TDelegate,TParams>>
        {
            public static PooledPolicy Instance { get; } = new PooledPolicy();
            public override RpcMethodClosure<TProxy, TDelegate, TParams> Create()
            {
                return new RpcMethodClosure<TProxy, TDelegate, TParams>();
            }

            public override bool Return(RpcMethodClosure<TProxy, TDelegate, TParams> obj)
            {
                obj.RpcMethodDictionary = null!;
                obj.Responser = null!;
                obj.RpcMethod = null!;
                obj.Params = default;
                obj.ID = default;
                
                return true;
            }


        }
        static DefaultObjectPool<RpcMethodClosure<TProxy, TDelegate, TParams>> Pool { get; set; } = new DefaultObjectPool<RpcMethodClosure<TProxy, TDelegate, TParams>>(PooledPolicy.Instance);
        #endregion
    }

    public readonly struct DefaultFunctionProxy<TDelegate, TParams,TResult, TInvoker> : JsonRpcMethodDictionary.RpcResponseCreater<TDelegate,TParams>
        where TInvoker:struct, JsonRpcMethodDictionary.IRpcFunctionInvoker<TDelegate,TParams,TResult>
                    where TDelegate : Delegate
            where TParams : IMethodParams
    {
        public void DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponser responser, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            TResult result;
            try
            {
                result = default(TInvoker).Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                if (id is ID requestID)
                {
                    responser.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                }
                return;
            }
            {
                if (id is ID requestID)
                {
                    responser.ResponseResult(new ResultResponse<TResult>(requestID, result));
                }
            }
        }

    }
    public readonly struct DefaultActionProxy<TDelegate, TParams, TInvoker> : JsonRpcMethodDictionary.RpcResponseCreater<TDelegate, TParams>
        where TInvoker : struct, JsonRpcMethodDictionary.IRpcActionInvoker<TDelegate, TParams>
                    where TDelegate : Delegate
            where TParams : IMethodParams
    {
        public void DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponser responser, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            try
            {
                default(TInvoker).Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                if (id is ID requestID)
                {
                    responser.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                }
                return;
            }
            {
                if (id is ID requestID)
                {
                    responser.ResponseResult(ResultResponse.Create(requestID));
                }
            }
        }

    }

    public readonly struct DefaultCancellableActionProxy<TDelegate, TParams, TInvoker>
        where TInvoker : struct, JsonRpcMethodDictionary.IRpcActionInvoker<TDelegate, TParams>
                    where TDelegate : Delegate
            where TParams : ICancellableMethodParams
    {
        public void DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponser responser, TDelegate rpcMethod, TParams parameters, ID? id = null)
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

                    default(TInvoker).Invoke(rpcMethod,parameters);
                }
                catch (Exception e)
                {
                    if (id is ID requestID)
                    {
                        if (e is OperationCanceledException)
                        {
                            responser.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));//TODO:キャンセルの別途ハンドリング
                        }
                        else
                        {
                            responser.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                        }
                    }
                    return;
                }
                {
                    if (id is ID requestID)
                    {
                        responser.ResponseResult(ResultResponse.Create(requestID));
                    }
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

    public readonly struct DefaultCancellableFunctionProxy<TDelegate, TParams, TResult, TInvoker> : JsonRpcMethodDictionary.RpcResponseCreater<TDelegate, TParams>
      where TInvoker : struct, JsonRpcMethodDictionary.IRpcFunctionInvoker<TDelegate, TParams, TResult>
                  where TDelegate : Delegate
          where TParams : ICancellableMethodParams
    {
        public void DelegateResponse(JsonRpcMethodDictionary methodDictionary, IResponser responser, TDelegate rpcMethod, TParams parameters, ID? id = null)
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

                    result = default(TInvoker).Invoke(rpcMethod, parameters);
                }
                catch (Exception e)
                {
                    if (id is ID requestID)
                    {
                        if (e is OperationCanceledException)
                        {
                            responser.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));//TODO:キャンセルの別途ハンドリング
                        }
                        else
                        {
                            responser.ResponseException(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                        }
                    }
                    return;
                }
                {
                    if (id is ID requestID)
                    {
                        responser.ResponseResult(ResultResponse.Create(requestID, result));
                    }
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
