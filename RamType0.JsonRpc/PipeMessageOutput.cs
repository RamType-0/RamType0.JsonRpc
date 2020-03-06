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
    using Marshaling;
    using Internal;
    public class PipeMessageOutput<T> : IDuplexOutput
        where T: notnull,IMessageWriter
    {
        T writer;
        public PipeMessageOutput(T writer,PipeWriter outputPipe)
        {
            this.writer = writer;
            Responses = Channel.CreateUnbounded<SendMessageHandle>(new UnboundedChannelOptions() { AllowSynchronousContinuations = false, SingleReader = true, SingleWriter = false });
            OutputPipe = outputPipe;
        }
        Channel<SendMessageHandle> Responses { get; }
        PipeWriter OutputPipe { get; }

        
        public async ValueTask StartOutputAsync(CancellationToken cancellationToken = default)
        {

            IAsyncEnumerator<SendMessageHandle>? enumerator = null;
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

        void WriteResponse(SendMessageHandle source)
        {
            var serializedResponse = source.SerializedMessage;
            var span = serializedResponse.AsSpan();
            try
            {
                writer.WriteMessage(OutputPipe, span);
                source.SendComplete();
            }
            catch (Exception e)
            {
                source.SetException(e);
            }
        }
        ValueTask IResponseOutput.ResponseAsync<TResponse>(Server.Server server, TResponse response)
        {
            var tmp = JsonSerializer.SerializeUnsafe(response).CopyToPooled();
            var source = SendMessageHandle.Create(tmp);
            _ = Responses.Writer.WriteAsync(source);
            return source.Task;
        }
        ValueTask IRequestOutput.SendRequestAsync<TParams>(Client.Client client, Request<TParams> request)
        {
            var tmp = JsonSerializer.SerializeUnsafe(request).CopyToPooled();
            var source = SendMessageHandle.Create(tmp);
            _ = Responses.Writer.WriteAsync(source);
            return source.Task;
        }

        ValueTask IRequestOutput.SendNotification<TParams>(Client.Client client, Notification<TParams> notification)
        {
            var tmp = JsonSerializer.SerializeUnsafe(notification).CopyToPooled();
            var source = SendMessageHandle.Create(tmp);
            _ = Responses.Writer.WriteAsync(source);
            return source.Task;
        }
    }

}
