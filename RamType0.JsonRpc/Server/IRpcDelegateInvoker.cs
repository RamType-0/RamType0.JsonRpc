using System;

namespace RamType0.JsonRpc.Server
{
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
}
