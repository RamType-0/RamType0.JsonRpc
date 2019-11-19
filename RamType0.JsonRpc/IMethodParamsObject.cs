using System;
using System.Threading;

namespace RamType0.JsonRpc
{
    /// <summary>
    /// 関数の引数を表現するオブジェクトを示します。Disposeすると、以後全ての呼び出しがNull参照になります。
    /// </summary>
    public interface IMethodParamsObject : IDisposable
    {
        public void Invoke();
        
    }

    /// <summary>
    /// 戻り値を持った関数の引数を表現する<see cref="IMethodParamsObject"/>を示します。
    /// </summary>
    /// <typeparam name="T">関数の戻り値の型。</typeparam>
    public interface IFunctionParamsObject<T> : IMethodParamsObject
    {
        public new T Invoke();
        void IMethodParamsObject.Invoke() => Invoke();
    }

    public interface ICancellableMethodParamsObject : IMethodParamsObject
    {
        public CancellationToken CancellationToken { get; set; }
    }

    public interface IActionParamsObject : IMethodParamsObject
    {

    }

    /// <summary>
    /// インスタンスフィールドを持たない<see cref="IMethodParamsObject"/>を示します。
    /// </summary>
    public interface IEmptyParamsObject : IMethodParamsObject { }

}
