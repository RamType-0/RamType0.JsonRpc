using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{

    public interface IRpcMethodBody<in TParams,out TResult>
    {
        public TResult Invoke(TParams parameters);
    }

    public interface IRpcMethodBody<in TParams> : IRpcMethodBody<TParams, NullResult>
    {
        public new void Invoke(TParams parameters);
        NullResult IRpcMethodBody<TParams,NullResult>.Invoke(TParams parameters)
        {
            Invoke(parameters);
            return new NullResult();
        }
    }

    public interface IRpcAsyncMethodBody<in TParams,TResult>
    {
        public ValueTask<TResult> Invoke(TParams parameters);
    }
    public interface IRpcAsyncMethodBody<in TParams>
    {
        public ValueTask Invoke(TParams parameters);
    }

    public interface IFunctionPointerContainer
    {
        public IntPtr FunctionPointer { set; }
    }

    public interface IObjectReferenceContainer 
    { 
        public object Target { set; }
    }

    public interface IDelegateContainer<in TDelegate>
        where TDelegate : notnull,Delegate
    {
        TDelegate Delegate { set; }
    }

    public interface IMethodParamsModifier<TParams>
    {
        public void Modify(ref TParams parameters, ArraySegment<byte> parametersSegment, ID? id, IJsonFormatterResolver formatterResolver);
    }

    public interface IRpcMethodEntry : IRpcAsyncMethodEntry
    {
        public ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver);
        ValueTask<ArraySegment<byte>> IRpcAsyncMethodEntry.ResolveRequestAsync(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver) => new ValueTask<ArraySegment<byte>>(ResolveRequest(serializedParameters, id, readFormatterResolver, writeFormatterResolver));
    }
    public interface IRpcAsyncMethodEntry
    {
        public ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver);
    }
}
