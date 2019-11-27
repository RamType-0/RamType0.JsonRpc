using Microsoft.Extensions.ObjectPool;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Utf8Json;

namespace RamType0.JsonRpc.Client
{
    public abstract class RequestCompletionSource 
    {
        public abstract void SetResult(ArraySegment<byte> resultSegment,IJsonFormatterResolver formatterResolver);
        public abstract void SetException(Exception exception);
    }

    public sealed class RequestCompletionSource<TResult> : RequestCompletionSource, IValueTaskSource<TResult>, IValueTaskSource
    {
        public static DefaultObjectPool<RequestCompletionSource<TResult>> Pool { get; } = new DefaultObjectPool<RequestCompletionSource<TResult>>(new PooledPolicy());
        public static RequestCompletionSource<TResult> Get()
        {
            return Pool.Get();
        }
        sealed class PooledPolicy : PooledObjectPolicy<RequestCompletionSource<TResult>>
        {
            public override RequestCompletionSource<TResult> Create()
            {
                return new RequestCompletionSource<TResult>();
            }

            public override bool Return(RequestCompletionSource<TResult> obj)
            {
                obj.core.Reset();
                return true;
            }
        }
        ManualResetValueTaskSourceCore<TResult> core;
        public TResult GetResult(short token)
        {
            try
            {
                return core.GetResult(token);
            }
            finally
            {
                Pool.Return(this);//レスポンス伝達を終えて家路へ向かうRequestCompletionSource<TResult>。疲れからか、不幸にも黒塗りのオブジェクトプールへ追突してしまう・・・
            }
        }
        public ValueTaskSourceStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            core.OnCompleted(continuation, state, token, flags);
        }
        void IValueTaskSource.GetResult(short token) => GetResult(token);

        public ValueTask<TResult> ValueTask
        {
            get
            {
                return new ValueTask<TResult>(this, core.Version);
            }
        }

        public ValueTask VoidValueTask
        {
            get
            {
                return new ValueTask(this, core.Version);
            }
        }

        public override void SetResult(ArraySegment<byte> resultSegment, IJsonFormatterResolver formatterResolver)
        {
            try
            {
                core.SetResult(JsonSerializer.Deserialize<TResult>(resultSegment.Array!, resultSegment.Offset, formatterResolver));
            }
            catch (Exception e)
            {
                core.SetException(e);
            }
        }
        public override void SetException(Exception exception)
        {
            core.SetException(exception);
        }
    }
}
