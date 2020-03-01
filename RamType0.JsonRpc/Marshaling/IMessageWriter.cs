using System;
using System.Threading.Tasks;
using System.IO.Pipelines;
using System.Threading;

namespace RamType0.JsonRpc.Marshaling
{
    public interface IMessageWriter
    {
        void WriteResponse(PipeWriter writer,ReadOnlySpan<byte> serializedResponse);
        /// <summary>
        /// 戻り値の<see cref="ValueTask"/>が完了、または例外をスローした時点で<paramref name="serializedResponse"/>は利用できなくなります。必要な場合、事前にコピーしてください。
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="serializedResponse"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        ValueTask WriteResponseAsync(PipeWriter writer, ReadOnlyMemory<byte> serializedResponse,CancellationToken cancellationToken = default)
        {
            WriteResponse(writer, serializedResponse.Span);
            return new ValueTask();
        }
    }
}
