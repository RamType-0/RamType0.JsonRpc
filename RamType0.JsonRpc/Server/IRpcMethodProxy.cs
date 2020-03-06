using System;
using System.Threading.Tasks;

namespace RamType0.JsonRpc.Server
{
    using Protocol;
    public interface IRpcMethodProxy<TDelegate, in TParams>
        where TDelegate : Delegate
        where TParams : IMethodParams
    {
        /// <summary>
        /// <paramref name="rpcMethod"/>に<paramref name="parameters"/>を引数に変換して呼び出し、<paramref name="id"/>が<see langword="null"/>でなければ呼び出し結果に基づいたレスポンスを生成、それを<paramref name="output"/>に伝えるまでを全て代行します。
        /// </summary>
        /// <param name="server">呼び出し元の<see cref="Server"/>。</param>
        /// <param name="output">レスポンスの最終的な出力を行う<see cref="IResponseOutput"/>。</param>
        /// <param name="rpcMethod"></param>
        /// <param name="parameters"></param>
        /// <param name="id"></param>
        ValueTask DelegateResponse(Server server, TDelegate rpcMethod, TParams parameters, ID? id = null);
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

        public ValueTask DelegateResponse(Server server, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            TResult result;
            var output = server.Output;
            try
            {
                result = Invoker.Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                return server.ExceptionHandler.OnException(server, id, e);
            }
            {
                if (id is ID requestID)
                {
                    try
                    {
                        return output.ResponseResult(server,new ResultResponse<TResult>(requestID, result));
                    }
                    catch (Exception e)
                    {
                        return server.ExceptionHandler.OnException(server, id, e);
                    }
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
        public ValueTask DelegateResponse(Server server, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            var output = server.Output;
            try
            {
                Invoker.Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                return server.ExceptionHandler.OnException(server, id, e);
            }
            {
                if (id is ID requestID)
                {
                    try
                    {
                        return output.ResponseResult(server,ResultResponse.Create(requestID));
                    }
                    catch (Exception e)
                    {
                        return server.ExceptionHandler.OnException(server, id, e);
                    }
                }
                return new ValueTask();
            }
        }

    }

    public readonly struct DefaultIDInjectedActionProxy<TDelegate, TParams, TInvoker> : IRpcMethodProxy<TDelegate, TParams>
        where TInvoker : notnull, IRpcActionInvoker<TDelegate, TParams>
                    where TDelegate : Delegate
            where TParams : IMethodParamsInjectID
    {
        public DefaultIDInjectedActionProxy(TInvoker invoker)
        {
            Invoker = invoker;
        }

        public TInvoker Invoker { get; }
        public ValueTask DelegateResponse(Server server, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            var output = server.Output;


            try
            {
                parameters.ID = id;

                Invoker.Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                return server.ExceptionHandler.OnException(server, id, e);
            }
            {
                if (id is ID requestID)
                {
                    try
                    {
                        return output.ResponseResult(server,ResultResponse.Create(requestID));
                    }
                    catch (Exception e)
                    {
                        return server.ExceptionHandler.OnException(server, id, e);
                    }
                }
                return new ValueTask();
            }



        }

    }

    public readonly struct DefaultIDInjectedFunctionProxy<TDelegate, TParams, TResult, TInvoker> : IRpcMethodProxy<TDelegate, TParams>
      where TInvoker : notnull, IRpcFunctionInvoker<TDelegate, TParams, TResult>
                  where TDelegate : Delegate
          where TParams : IMethodParamsInjectID
    {
        public DefaultIDInjectedFunctionProxy(TInvoker invoker)
        {
            Invoker = invoker;
        }

        public TInvoker Invoker { get; }
        public ValueTask DelegateResponse(Server server, TDelegate rpcMethod, TParams parameters, ID? id = null)
        {
            var output = server.Output;

            TResult result;

            try
            {
                parameters.ID = id;

                result = Invoker.Invoke(rpcMethod, parameters);
            }
            catch (Exception e)
            {
                return server.ExceptionHandler.OnException(server, id, e);
            }
            {
                if (id is ID requestID)
                {
                    try
                    {
                        return output.ResponseResult(server,ResultResponse.Create(requestID, result));
                    }
                    catch (Exception e)
                    {
                        return server.ExceptionHandler.OnException(server, id, e);
                    }
                }
                return new ValueTask();
            }


        }

    }
}
