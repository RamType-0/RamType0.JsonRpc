using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace RamType0.JsonRpc
{
    class SendMessageCompletionSource : IValueTaskSource
    {
        public static SendMessageCompletionSource Create(ReadOnlySpan<byte> serializedResponse)
        {
            var source = Pool.Get();
            var buffer = ArrayPool<byte>.Shared.Rent(serializedResponse.Length);
            serializedResponse.CopyTo(buffer);

            source.serializedResponse = new ArraySegment<byte>(buffer, 0, serializedResponse.Length);
            return source;
        }
        static ObjectPool<SendMessageCompletionSource> Pool { get; } = new DefaultObjectPool<SendMessageCompletionSource>(new PoolPolicy());

        sealed class PoolPolicy : PooledObjectPolicy<SendMessageCompletionSource>
        {
            public override SendMessageCompletionSource Create()
            {
                return new SendMessageCompletionSource();
            }

            public override bool Return(SendMessageCompletionSource obj)
            {
                obj.core.Reset();
                
                return true;
            }
        }

        ManualResetValueTaskSourceCore<NullResult> core = new ManualResetValueTaskSourceCore<NullResult>();
        ArraySegment<byte> serializedResponse;

        

        public ref readonly ArraySegment<byte> SerializedResponse => ref serializedResponse;
        public void GetResult(short token)
        {
            try
            {

                core.GetResult(token);
            }
            finally
            {
                Pool.Return(this);
            }
        }

        public ValueTaskSourceStatus GetStatus(short token)
        {
            return core.GetStatus(token);
        }

        public ValueTask Task => new ValueTask(this, core.Version);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        {
            core.OnCompleted(continuation, state, token, flags);
        }

        public void SetComplete()
        {
            core.SetResult(new NullResult());
            FreeSerializedResponse();
        }
        /// <summary>
        /// SetCompleteかSetException経由でしか呼ばれないので二重開放はされない
        /// </summary>
        private void FreeSerializedResponse()
        {
            ArrayPool<byte>.Shared.Return(serializedResponse.Array!);
            serializedResponse = default!;
        }

        public void SetException(Exception exception)
        {
            core.SetException(exception);
            FreeSerializedResponse();
        }
    }
}
