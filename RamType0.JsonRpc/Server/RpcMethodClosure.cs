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

namespace RamType0.JsonRpc.Server
{
    public sealed class RpcMethodClosure<TProxy, TDelegate, TParams>
         where TProxy : notnull,IRpcMethodProxy<TDelegate, TParams>
            where TDelegate : Delegate
            where TParams : IMethodParams
    {
        public JsonRpcMethodDictionary RpcMethodDictionary { get; private set; } = default!;

        public IResponseOutput Output { get; private set; } = default!;
        public TProxy Proxy { get; private set; } = default!;
        public TDelegate RpcMethod { get; private set; } = default!;
        public TParams Params { get; private set; } = default!;
        public ID? ID { get; private set; }
        public Action InvokeAction { get; }
        public void InvokeWithPoolingAndLogging()
        {
            try
            {
                _ = Invoke(RpcMethodDictionary, Output,Proxy,RpcMethod, Params, ID);
            }
            finally
            {
                Pool.Return(this);
            }
        }

        public static ValueTask Invoke(JsonRpcMethodDictionary methodDictionary, IResponseOutput output,TProxy proxy, TDelegate rpcMethod, TParams parameters, ID? id)
        {
            
            return proxy.DelegateResponse(methodDictionary, output,rpcMethod ,parameters, id);
            
        }

        static byte[] ErrorLogHeader { get; } = Encoding.UTF8.GetBytes($"Unhandled exception from {typeof(TProxy).Name}. \n");
        private RpcMethodClosure()
        {
            InvokeAction = InvokeWithPoolingAndLogging;
        }

        /// <summary>
        /// プーリングを行いつつ新たなクロージャを取得します。プーリング機構が破棄されている場合、プーリングは行われません。
        /// </summary>
        /// <param name="output"></param>
        /// <param name="parameters"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public static RpcMethodClosure<TProxy, TDelegate, TParams> GetClosure(JsonRpcMethodDictionary methodDictionary, IResponseOutput output,TProxy proxy,TDelegate rpcMethod, TParams parameters, ID? id)
        {
            var closure = Pool.Get();
            closure.Inject(methodDictionary, output,proxy,rpcMethod, parameters, id);
            return closure;
        }



        

        void Inject(JsonRpcMethodDictionary methodDictionary, IResponseOutput output,TProxy proxy,TDelegate rpcMethod ,TParams parameters, ID? id)
        {
            RpcMethodDictionary = methodDictionary;
            Output = output;
            Proxy = proxy;
            RpcMethod = rpcMethod;
            Params = parameters;
            ID = id;
        }


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
                obj.Output = null!;
                obj.Proxy = default!;
                obj.RpcMethod = null!;
                obj.Params = default!;
                obj.ID = default;
                
                return true;
            }


        }
        static DefaultObjectPool<RpcMethodClosure<TProxy, TDelegate, TParams>> Pool { get; set; } = new DefaultObjectPool<RpcMethodClosure<TProxy, TDelegate, TParams>>(PooledPolicy.Instance);
        #endregion
    }


}
