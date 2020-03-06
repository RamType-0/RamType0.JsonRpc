using RamType0.JsonRpc.Client;
using RamType0.JsonRpc.Marshaling;
using RamType0.JsonRpc.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    public abstract class RpcDomain
    {
        public RpcDomain():this(Channel.CreateUnbounded<MessageHandle>(new UnboundedChannelOptions() { AllowSynchronousContinuations = true,SingleReader = true,SingleWriter = false})) { }
        internal RpcDomain(Channel<MessageHandle> messageChannel)
        {
            MessageChannel = messageChannel;
        }

        long id;


        ConcurrentDictionary<long, RequestHandle> UnResponsedRequests { get; } = new ConcurrentDictionary<long, RequestHandle>();

        public ConcurrentBag<string> UnknownErrors { get; } = new ConcurrentBag<string>();

        private protected Channel<MessageHandle> MessageChannel { get; }
        public ValueTask RequestAsync<TParams>(EscapedUTF8String methodName, TParams parameters, IJsonFormatterResolver formatterResolver)
        {
            var handle = RequestAsyncCore<TParams,NullResult>(methodName, parameters, formatterResolver);
            return handle.TaskVoid;
        }

        
        public ValueTask<TResult> RequestAsync<TParams,TResult>(EscapedUTF8String methodName,TParams parameters, IJsonFormatterResolver formatterResolver)
        {
            var handle = RequestAsyncCore<TParams, TResult>(methodName, parameters, formatterResolver);
            return handle.Task;
        }

        private RequestHandle<TResult> RequestAsyncCore<TParams, TResult>(EscapedUTF8String methodName, TParams parameters,IJsonFormatterResolver formatterResolver)
        {
            var id = Interlocked.Increment(ref this.id);
            
            var serializedRequest = JsonSerializer.SerializeUnsafe(new Request<TParams>() { ID = new ID(id), Method = methodName, Params = parameters },formatterResolver).CopyToPooled();
            var handle = RequestHandle<TResult>.Create(serializedRequest);
            _ = MessageChannel.Writer.WriteAsync(handle);

            if(!UnResponsedRequests.TryAdd(id, handle))
            {
                Debug.Fail("ID conflicted.");
            }
            
            return handle;
        }

        public ValueTask NotifyAsync<TParams>(EscapedUTF8String methodName, TParams parameters, IJsonFormatterResolver formatterResolver)
        {
            var serializedNotification = JsonSerializer.SerializeUnsafe(new Notification<TParams>() { Method = methodName, Params = parameters },formatterResolver).CopyToPooled();
            var handle = SendMessageHandle.Create(serializedNotification);
            _ = MessageChannel.Writer.WriteAsync(handle);
            return handle.Task;
        }

        public void SetResult(ID id, ArraySegment<byte> resultJson)
        { 
            if(id.Number is long num && UnResponsedRequests.TryGetValue(num, out var handle))
            {
                handle.SetResult(resultJson);
            }
            else
            {
                Debug.Fail("Received unknown id response.");
            }
            
        }

        public void SetError(ID? id, ArraySegment<byte> errorSegment)
        {
            if(id is ID reqID && reqID.Number is long num && UnResponsedRequests.TryGetValue(num,out var handle))
            {
                handle.SetError(errorSegment);
            }
            else
            {
                UnknownErrors.Add(Encoding.UTF8.GetString(errorSegment));
            }
        }

        

        public ValueTask ResponseAsync(ArraySegment<byte> pooledSerializedResponse)
        {
            var handle = SendMessageHandle.Create(pooledSerializedResponse);
            _ = MessageChannel.Writer.WriteAsync(handle);
            return handle.Task;
        }

    }

    public sealed class RpcDomain<T> : RpcDomain
        where T:notnull,IMessageWriter
    {
        T messageWriter;
        PipeWriter MessageOutputPipe { get; }


        public RpcDomain(T messageWriter, PipeWriter messageOutputPipe)
        {
            this.messageWriter = messageWriter;
            MessageOutputPipe = messageOutputPipe;
        }
        internal async ValueTask SendMessageAsyncThroughPut(CancellationToken cancellationToken)
        {
            IAsyncEnumerator<MessageHandle>? enumerator = null;
            try
            {
                
                enumerator = MessageChannel.Reader.ReadAllAsync(cancellationToken).GetAsyncEnumerator();
                var unFlushedMessages = new List<MessageHandle>();
                while (true)
                {
                    var moveNext = enumerator.MoveNextAsync();
                    if (moveNext.IsCompleted)
                    {
                        if (moveNext.Result)
                        {
                            var message = enumerator.Current;
                            await messageWriter.WriteMessageAsync(MessageOutputPipe, message.SerializedMessage, cancellationToken);
                            unFlushedMessages.Add(message);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        await MessageOutputPipe.FlushAsync(cancellationToken);
                        foreach (var message in unFlushedMessages)
                        {
                            message.SendComplete();
                        }
                        unFlushedMessages.Clear();
                        if (await moveNext)
                        {
                            var message = enumerator.Current;
                            await messageWriter.WriteMessageAsync(MessageOutputPipe, message.SerializedMessage, cancellationToken);
                            unFlushedMessages.Add(message);
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
                if (!(enumerator is null))
                {
                    await enumerator.DisposeAsync();
                }
            }

            
        }
        public async ValueTask SendMessageAsync(CancellationToken cancellationToken = default)
        {
            await foreach(var message in MessageChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await messageWriter.WriteMessageAsync(MessageOutputPipe, message.SerializedMessage, cancellationToken);
                var flushResult = await MessageOutputPipe.FlushAsync(cancellationToken);
                if (flushResult.IsCanceled)
                {
                    break;
                }
                message.SendComplete();
            }
        }
    }

    

}
