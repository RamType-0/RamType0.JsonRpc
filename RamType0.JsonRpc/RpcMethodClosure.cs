using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RamType0.JsonRpc.Emit;

namespace RamType0.JsonRpc
{
    /// <summary>
    /// ラムダ式使ってFunc作るときに作られるクロージャをカスタム実装してプーリング！
    /// </summary>
    /// <typeparam name="T"></typeparam>
    abstract class RpcMethodClosure<T>
            where T : struct, IMethodParamsObject
    {
        /// <summary>
        /// プーリングを行いつつ新たなクロージャを取得します。プーリング機構が破棄されている場合、プーリングは行われません。
        /// </summary>
        /// <param name="responser"></param>
        /// <param name="parameters"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static RpcMethodClosure<T> GetClosure(IResponser responser, T parameters, ID? id)
        {
            
            if(Pool is null || !Pool.TryTake(out var closure))
            {
                closure = Factory();
            }
            closure.InjectParams(responser, parameters, id);
            return closure;
        }

        private static Func<RpcMethodClosure<T>> Factory { get; } = GetFactory();

        protected RpcMethodClosure(IResponser responser, T parameters, ID? id) : this()
        {
            InjectParams(responser, parameters, id);
        }

        static Func<RpcMethodClosure<T>> GetFactory()
        {
            var interfaces = typeof(T).GetInterfaces();
            foreach (var i in interfaces)
            {
                if (i.IsConstructedGenericType && i.GetGenericTypeDefinition() == typeof(IMethodParamsObject<>))
                {
                    var factoryMethod = typeof(RpcMethodClosure<,>).MakeGenericType(typeof(T), i.GetGenericArguments()[0]).GetMethod("GetInstance", Type.EmptyTypes)!;
                    return Unsafe.As<Func<RpcMethodClosure<T>>>(factoryMethod.CreateDelegate(typeof(Func<RpcMethodClosure<T>>)));
                }
            }
            return VoidRpcMethodClosure<T>.GetInstance;
        }


        protected RpcMethodClosure()
        {
            InvokeAction = Invoke;
        }

        public void InjectParams(IResponser responser, T parameters, ID? id)
        {
            Responser = responser;
            Params = parameters;
            ID = id;
        }

        protected static ConcurrentBag<RpcMethodClosure<T>>? Pool { get; private set; } = new ConcurrentBag<RpcMethodClosure<T>>();

        /// <summary>
        /// プーリング機構を破棄し、プーリングに使われるリソースを開放します。GetClosure自体は有効のままになります。
        /// </summary>
        public static void Dispose()
        {
            Pool = null;
        }
        /// <summary>
        /// プーリング機構が破棄されていない場合、プールされている全てのクロージャを開放します。プーリング機構が破棄されていた場合、何も行いません。
        /// </summary>
        public static void ReleasePooledClosures()
        {
            Pool?.Clear();
        }

        public IResponser Responser { get; private set; } = default!;
        public T Params { get; private set; }
        public ID? ID { get; private set; }
        protected abstract void Invoke();
        public Action InvokeAction { get; }
    }
    sealed class VoidRpcMethodClosure<T> : RpcMethodClosure<T>
        where T : struct, IMethodParamsObject
    {
        public static VoidRpcMethodClosure<T> GetInstance()
        {
            return new VoidRpcMethodClosure<T>();
        }
        protected override void Invoke()
        {
            try
            {
                try
                {
                    Params.Invoke();
                }
                catch (Exception e)
                {
                    if (ID is ID requestID)
                    {
                        Responser.Response(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                    }
                    return;
                }
                {
                    if (ID is ID requestID)
                    {
                        Responser.Response(ResultResponse.Result(requestID));
                    }
                }
            }
            finally
            {
                Pool?.Add(this);
            }
        }
    }

    sealed class RpcMethodClosure<TParams, TResult> : RpcMethodClosure<TParams>
        where TParams : struct, IMethodParamsObject<TResult>
    {
        public static RpcMethodClosure<TParams, TResult> GetInstance()
        {
            return new RpcMethodClosure<TParams, TResult>();
        }
        protected override void Invoke()
        {
            try
            {
                TResult result;
                try
                {
                    result = Params.Invoke();
                }
                catch (Exception e)
                {
                    if (ID is ID requestID)
                    {
                        Responser.Response(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e));
                    }
                    return;
                }
                {
                    if (ID is ID requestID)
                    {
                        Responser.Response(new ResultResponse<TResult>(requestID, result));
                    }
                }
            }
            finally
            {
                Pool?.Add(this);
            }
        }
    }
}
