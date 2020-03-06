using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Utf8Json;
using Utf8Json.Resolvers;

namespace RamType0.JsonRpc.Internal
{
    abstract class RequestHandle : MessageHandle 
    {
        public IErrorHandler ErrorHandler { get; set; } = null!;
        public abstract void SetResult(ArraySegment<byte> resultSegment);

        public void SetError(ArraySegment<byte> errorSegment)
        {
            SetException(ErrorHandler.AsException(errorSegment));
        }

    }
    sealed class RequestHandle<TResult> : RequestHandle, IValueTaskSource, IValueTaskSource<TResult>
    {
        public static IJsonFormatterResolver ResultFormatterResolver { get; set; } = StandardResolver.ExcludeNullCamelCase;
        internal static RequestHandle<TResult> Create(ArraySegment<byte> pooledArraySegment,IErrorHandler errorHandler)
        {
            var handle = Pool.Get();
            handle.serializedMessage = pooledArraySegment;
            handle.ErrorHandler = errorHandler;
            return handle;
        }

        internal static RequestHandle<TResult> Create(ArraySegment<byte> pooledArraySegment) => Create(pooledArraySegment, DefaultErrorHandler.Instance);

        static DefaultObjectPool<RequestHandle<TResult>> Pool { get; } = new DefaultObjectPool<RequestHandle<TResult>>(new PoolPolicy());
        sealed class PoolPolicy : PooledObjectPolicy<RequestHandle<TResult>>
        {
            public override RequestHandle<TResult> Create()
            {
                return new RequestHandle<TResult>();
            }

            public override bool Return(RequestHandle<TResult> obj)
            {
                obj.ErrorHandler = null!;
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
                Pool.Return(this);
            }
            
        }

        public ValueTaskSourceStatus GetStatus(short token) => core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => core.OnCompleted(continuation, state, token, flags);

        void IValueTaskSource.GetResult(short token) => GetResult(token);

        public override void SetResult(ArraySegment<byte> resultSegment)
        {
            var reader = new JsonReader(resultSegment.Array, resultSegment.Offset);
            TResult result;
            try
            {
                result = JsonSerializer.Deserialize<TResult>(ref reader, ResultFormatterResolver);
            }
            catch (Exception e)
            {

                SetException(e);
                return;
            }
            core.SetResult(result);
            
        }

        public override void SetException(Exception e)
        {
            core.SetException(e);
        }

        private protected override void OnSendComplete()
        {
            return;
        }

        public ValueTask<TResult> Task => new ValueTask<TResult>(this, core.Version);
        public ValueTask TaskVoid => new ValueTask(this, core.Version);

    }
}
