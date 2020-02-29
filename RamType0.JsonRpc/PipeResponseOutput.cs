using RamType0.JsonRpc.Client;
using RamType0.JsonRpc.Duplex;
using RamType0.JsonRpc.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Utf8Json;
using System.Buffers;
using System.Threading.Channels;
using System.Threading;

namespace RamType0.JsonRpc
{
    public class PipeResponseOutput<T> : IResponseOutput
        where T: notnull,IResponseWriter
    {
        T writer;
        public PipeResponseOutput(T writer,PipeWriter outputPipe)
        {
            this.writer = writer;
            Responses = Channel.CreateUnbounded<ResponseCompletionSource>(new UnboundedChannelOptions() { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = false });
            OutputPipe = outputPipe;
        }
        Channel<ResponseCompletionSource> Responses { get; }
        PipeWriter OutputPipe { get; }

        ValueTask IResponseOutput.ResponseAsync<TResponse>(Server.Server server, TResponse response)
        {
            var tmp = JsonSerializer.SerializeUnsafe(response);
            var source = ResponseCompletionSource.Create(tmp);
            _ = Responses.Writer.WriteAsync(source);
            return source.Task;
        }
        public async ValueTask StartOutputAsync(CancellationToken cancellationToken = default)
        {

            IAsyncEnumerator<ResponseCompletionSource>? enumerator = null;
            try
            {
                enumerator = Responses.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator();
                while (true)
                {
                    var moveNext = enumerator.MoveNextAsync();
                    if (moveNext.IsCompleted)
                    {
                        if (moveNext.Result)
                        {
                            WriteResponse(enumerator.Current);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        await OutputPipe.FlushAsync(cancellationToken);
                        if (await moveNext)
                        {
                            WriteResponse(enumerator.Current);
                        }
                        else
                        {
                            break;
                        }
                    }


                }
            }
            finally
            {
                if(!(enumerator is null))
                {
                    await enumerator.DisposeAsync();
                }
            }
        }

        void WriteResponse(ResponseCompletionSource source)
        {
            var serializedResponse = source.SerializedResponse;
            var span = serializedResponse.AsSpan();
            try
            {
                writer.WriteResponse(OutputPipe, span);
                source.SetComplete();
            }
            catch (Exception e)
            {
                source.SetException(e);
            }
        }

    }

    public struct PassThroughWriter : IResponseWriter
    {
        public void WriteResponse(PipeWriter writer,ReadOnlySpan<byte> serializedResponse)
        {
            serializedResponse.CopyTo(writer.GetSpan(serializedResponse.Length));
            writer.Advance(serializedResponse.Length);
        }
    }

    public interface IResponseWriter
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
