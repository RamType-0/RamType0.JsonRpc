
using System;
using System.Buffers;

namespace RamType0.JsonRpc
{
    public abstract class MessageHandle
    {

        public void SendComplete()
        {
            var array = serializedMessage.Array!;
            ArrayPool<byte>.Shared.Return(array);
            serializedMessage = ArraySegment<byte>.Empty;
            OnSendComplete();


        }

        private protected abstract void OnSendComplete();

        public abstract void SetException(Exception e);

        public ref readonly ArraySegment<byte> SerializedMessage => ref serializedMessage;
        protected ArraySegment<byte> serializedMessage;
    }
}
