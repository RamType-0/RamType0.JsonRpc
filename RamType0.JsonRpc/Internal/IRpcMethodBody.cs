using System;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{

    public interface IRpcMethodBody<in TParams, TResult> : IRpcAsyncMethodBody<TParams, TResult>
    {
        public TResult Invoke(TParams parameters);
        ValueTask<TResult> IRpcAsyncMethodBody<TParams, TResult>.InvokeAsync(TParams parameters) => new ValueTask<TResult>(Invoke(parameters));
    }

    public interface IRpcMethodBody<in TParams> : IRpcMethodBody<TParams, NullResult>
    {
        public new void Invoke(TParams parameters);
        NullResult IRpcMethodBody<TParams, NullResult>.Invoke(TParams parameters)
        {
            Invoke(parameters);
            return new NullResult();
        }
    }

    public interface IRpcAsyncMethodBody<in TParams, TResult>
    {
        public ValueTask<TResult> InvokeAsync(TParams parameters);
    }
    public interface IRpcAsyncMethodBody<in TParams>
    {
        public ValueTask InvokeAsync(TParams parameters);
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
        where TDelegate : notnull, Delegate
    {
        TDelegate Delegate { set; }
    }

    public interface IMethodParamsModifier<TParams>
    {
        public void Modify(ref TParams parameters, ArraySegment<byte> parametersSegment, ID? id, IJsonFormatterResolver formatterResolver);
    }


}
