using System;

namespace RamType0.JsonRpc.Internal.Emit
{
    internal abstract class RpcAsyncMethodEntryFactory<T>
   where T : notnull, Delegate
    {
        public abstract RpcAsyncMethodEntry CreateAsyncMethodEntry(T d, IExceptionHandler exceptionHandler);
    }
    internal abstract class RpcMethodEntryFactory<T> : RpcAsyncMethodEntryFactory<T>
   where T : notnull, Delegate
    {
        public abstract RpcMethodEntry CreateMethodEntry(T d, IExceptionHandler exceptionHandler);
        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(T d, IExceptionHandler exceptionHandler)
        {
            return CreateMethodEntry(d, exceptionHandler);
        }
    }
    internal sealed class RpcInstanceMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcMethodEntryFactory<TDelegate>

        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IFunctionPointerContainer, IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {


        public override RpcMethodEntry CreateMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

    internal sealed class RpcInstanceAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>

        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams, TResult>, IFunctionPointerContainer, IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {



        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

    internal sealed class RpcInstanceAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>

        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams>, IFunctionPointerContainer, IObjectReferenceContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {

        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            method.Target = d.Target!;
            return new RpcAsyncMethodEntry<TMethod, TParams, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

    internal sealed class RpcStaticMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IFunctionPointerContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {



        public override RpcMethodEntry CreateMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

    internal sealed class RpcStaticAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams, TResult>, IFunctionPointerContainer
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {


        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }
    internal sealed class RpcStaticAsyncMethodEntryFactory<TDelegate, TMethod, TParams, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
       where TDelegate : notnull, Delegate
       where TMethod : struct, IRpcAsyncMethodBody<TParams>, IFunctionPointerContainer
       where TParams : notnull
       where TDeserializer : struct, IParamsDeserializer<TParams>
       where TModifier : struct, IMethodParamsModifier<TParams>
    {


        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.FunctionPointer = d.Method.MethodHandle.GetFunctionPointer();
            return new RpcAsyncMethodEntry<TMethod, TParams, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

    internal sealed class RpcDelegateEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcMethodBody<TParams, TResult>, IDelegateContainer<Delegate>
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        public override RpcMethodEntry CreateMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.Delegate = d;
            return new RpcMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

    internal sealed class RpcAsyncDelegateEntryFactory<TDelegate, TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams, TResult>, IDelegateContainer<Delegate>
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.Delegate = d;
            return new RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

    internal sealed class RpcAsyncDelegateEntryFactory<TDelegate, TMethod, TParams, TDeserializer, TModifier> : RpcAsyncMethodEntryFactory<TDelegate>
        where TDelegate : notnull, Delegate
        where TMethod : struct, IRpcAsyncMethodBody<TParams>, IDelegateContainer<Delegate>
        where TParams : notnull
        where TDeserializer : struct, IParamsDeserializer<TParams>
        where TModifier : struct, IMethodParamsModifier<TParams>
    {
        public override RpcAsyncMethodEntry CreateAsyncMethodEntry(TDelegate d, IExceptionHandler exceptionHandler)
        {
            var method = default(TMethod);
            method.Delegate = d;
            return new RpcAsyncMethodEntry<TMethod, TParams, TDeserializer, TModifier>(method, default, default, exceptionHandler);
        }
    }

}
