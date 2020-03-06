using System;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Threading;

namespace RamType0.JsonRpc.Marshaling
{
    public interface IMessageWriter
    {
        void WriteMessage(PipeWriter writer,ReadOnlySpan<byte> serializedMessage);
        /// <summary>
        /// 戻り値の<see cref="ValueTask"/>が完了、または例外をスローした時点で<paramref name="serializedMessage"/>は利用できなくなります。必要な場合、事前にコピーしてください。
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="serializedMessage"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask WriteMessageAsync(PipeWriter writer, ReadOnlyMemory<byte> serializedMessage,CancellationToken cancellationToken = default)
        {
            WriteMessage(writer, serializedMessage.Span);
            return new ValueTask();
        }
    }
}
