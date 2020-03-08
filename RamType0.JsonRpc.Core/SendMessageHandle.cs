using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace RamType0.JsonRpc
{
    sealed class SendMessageHandle : MessageHandle, IValueTaskSource
    {
        public static SendMessageHandle Create(ReadOnlySpan<byte> serializedMessage)
        {

            var buffer = ArrayPool<byte>.Shared.Rent(serializedMessage.Length);
            serializedMessage.CopyTo(buffer);
            var segment = new ArraySegment<byte>(buffer, 0, serializedMessage.Length);
            return Create(segment);
        }

        internal static SendMessageHandle Create(ArraySegment<byte> pooledArraySegment)
        {
            var source = Pool.Get();
            source.serializedMessage = pooledArraySegment;
            return source;
        }

        static ObjectPool<SendMessageHandle> Pool { get; } = new DefaultObjectPool<SendMessageHandle>(new PoolPolicy());

        sealed class PoolPolicy : PooledObjectPolicy<SendMessageHandle>
        {
            public override SendMessageHandle Create()
            {
                return new SendMessageHandle();
            }

            public override bool Return(SendMessageHandle obj)
            {
                obj.core.Reset();

                return true;
            }
        }

        ManualResetValueTaskSourceCore<NullResult> core = new ManualResetValueTaskSourceCore<NullResult>();
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


        private protected override void OnSendComplete()
        {
            core.SetResult(new NullResult());
        }

        public override void SetException(Exception exception)
        {
            core.SetException(exception);
        }
    }
}
