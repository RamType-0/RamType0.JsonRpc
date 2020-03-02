using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    public interface IRpcMethodBodyVoid<TParams>
    {
        public void Invoke(TParams parameters);
    }

    public interface IRpcMethodBody<TParams,TResult> : IRpcMethodBodyVoid<TParams>
    {
        public new TResult Invoke(TParams parameters);
        void IRpcMethodBodyVoid<TParams>.Invoke(TParams parameters) => Invoke(parameters);
    }

    public interface IFunctionPointerContainer
    {
        public IntPtr FunctionPointer { set; }
    }

    public interface IObjectReferenceContainer 
    { 
        public object Target { set; }
    }

    public interface IMulticastDelegateContainer<TDelegate>
        where TDelegate : notnull,MulticastDelegate
    {
        TDelegate Delegate { set; }
    }

    public interface IMethodParamsModifier<TParams>
        where TParams :notnull
    {
        public void Modify(ref TParams parameters, ArraySegment<byte> parametersSegment, ID? id, IJsonFormatterResolver formatterResolver);
    }

    public struct VoidRpcMethodWrapper<TMethod, TParams> : IRpcMethodBody<TParams, NullResult>
        where TMethod : IRpcMethodBodyVoid<TParams>
    {
        TMethod rpcMethod;

        public VoidRpcMethodWrapper(TMethod rpcMethod)
        {
            this.rpcMethod = rpcMethod;
        }

        public NullResult Invoke(TParams parameters)
        {
            rpcMethod.Invoke(parameters);
            return new NullResult();
        }
    }

    public interface IRpcEntry
    {
        public ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver formatterResolver);
    }
    
}
