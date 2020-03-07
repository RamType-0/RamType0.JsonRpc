using RamType0.JsonRpc.Client;
using RamType0.JsonRpc.Protocol;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;

namespace RamType0.JsonRpc.Internal
{
    public class RpcDomain
    {
        public RpcDomain() : this(Channel.CreateUnbounded<MessageHandle>(new UnboundedChannelOptions() { AllowSynchronousContinuations = true, SingleReader = true, SingleWriter = false })) { }
        public RpcDomain(Channel<MessageHandle> messageChannel) : this(messageChannel,StandardResolver.CamelCase) { }
        public RpcDomain(Channel<MessageHandle> messageChannel, IJsonFormatterResolver formatterResolver)
        {
            MessageChannel = messageChannel;
            JsonFormatterResolver = formatterResolver;
        }

        long id;
        public IJsonFormatterResolver JsonFormatterResolver { get; set; }
        ConcurrentDictionary<EscapedUTF8String, RpcAsyncMethodEntry> MethodEntries { get; } = new ConcurrentDictionary<EscapedUTF8String, RpcAsyncMethodEntry>();
        public bool AddMethod(string name, RpcAsyncMethodEntry methodEntry)
        {
            return MethodEntries.TryAdd(EscapedUTF8String.FromUnEscaped(name), methodEntry);
        }
        public bool RemoveMethod(string name, [NotNullWhen(true)]out RpcAsyncMethodEntry? methodEntry)
        {
            return MethodEntries.TryRemove(EscapedUTF8String.FromUnEscaped(name), out methodEntry);
        }


        
        public ValueTask __ResolveMessageAsync(ArraySegment<byte> message)
        {
            ValueTask task;
            var parseResult = MessageParser.ParseDuplexMessage(message);
            var messageKind = parseResult.MessageKind;
            switch (messageKind)
            {
                case MessageKind.InvalidJson:
                    {
                        var response = ErrorResponse.ParseError(message);
                        task = ResponseAsync(JsonSerializer.SerializeUnsafe(response, JsonFormatterResolver).CopyToPooled());
                        goto ReleaseMessage;
                    }
                case MessageKind.InvalidMessage:
                    {
                        var response = ErrorResponse.InvalidRequest(message);
                        task = ResponseAsync(JsonSerializer.SerializeUnsafe(response, JsonFormatterResolver).CopyToPooled());
                        goto ReleaseMessage;
                    }
                case MessageKind.ClientMessage:
                    {
                        if (MethodEntries.TryGetValue(parseResult.Method, out var entry))
                        {
                            var parameters = parseResult.Params;
                            var id = parseResult.id;
                            task = new ValueTask(Task.Run(async () =>
                            {
                                var response = await entry.ResolveRequestAsync(parameters, id);
                                ArrayPool<byte>.Shared.Return(message.Array!);
                                if (!(response.Array is null))
                                {
                                    await ResponseAsync(response);
                                }


                            }));
                            break;
                        }
                        else
                        {
                            if (parseResult.id is ID reqID)
                            {
                                var response = JsonSerializer.SerializeUnsafe(ErrorResponse.MethodNotFound(reqID, parseResult.Method.ToString())).CopyToPooled();
                                task = ResponseAsync(response);
                            }
                            else
                            {
                                task = new ValueTask();
                            }
                            goto ReleaseMessage;
                        }
                    }
                case MessageKind.ResultResponse:
                    {
                        if (parseResult.id is ID id)
                        {
                            SetResult(id, parseResult.Result);
                        }
                        else
                        {
                            Debug.Fail("Result without id");
                        }
                        task = new ValueTask();
                        goto ReleaseMessage;

                    }
                case MessageKind.ErrorResponse:
                    {
                        SetError(parseResult.id, parseResult.Error);
                        task = new ValueTask();
                        goto ReleaseMessage;
                    }
                default:
                    {
                        Debug.Fail($"Unknown {nameof(MessageKind)}:{messageKind.ToString()}");
                        task = new ValueTask();
                    }
                ReleaseMessage:
                    {
                        ArrayPool<byte>.Shared.Return(message.Array!);
                        break;
                    }
            }
            return task;
        }
        /// <summary>
        /// メッセージを非同期で解決します。このメソッドはスレッドセーフであり、非同期での解決完了を待つことなく他のメッセージの解決を行うことができます。
        /// </summary>
        /// <param name="message"></param>
        /// <returns>メッセージの解決完了を表す<see cref="ValueTask"/>。この<see cref="ValueTask"/>の完了を待つことなく、他のメッセージの解決を開始することができます。</returns>
        public async ValueTask ResolveMessageAsync(ArraySegment<byte> message)
        {
            var parseResult = MessageParser.ParseDuplexMessage(message);
            var messageKind = parseResult.MessageKind;
            switch (messageKind)
            {
                case MessageKind.InvalidJson:
                    {
                        var response = ErrorResponse.ParseError(message);
                        var serializedResponse = JsonSerializer.SerializeUnsafe(response, JsonFormatterResolver).CopyToPooled();
                        ArrayPool<byte>.Shared.Return(message.Array!);
                        await ResponseAsync(serializedResponse);
                        
                        return;
                    }
                case MessageKind.InvalidMessage:
                    {
                        var response = ErrorResponse.InvalidRequest(message);
                        var serializedResponse = JsonSerializer.SerializeUnsafe(response, JsonFormatterResolver).CopyToPooled();
                        ArrayPool<byte>.Shared.Return(message.Array!);
                        await ResponseAsync(serializedResponse);
                        return;
                    }
                case MessageKind.ClientMessage:
                    {
                        if (MethodEntries.TryGetValue(parseResult.Method, out var entry))
                        {
                            var parameters = parseResult.Params;
                            var id = parseResult.id;
                            
                            var response = await entry.ResolveRequestAsync(parameters, id);
                            ArrayPool<byte>.Shared.Return(message.Array!);
                            if (!(response.Array is null))
                            {
                                    await ResponseAsync(response);
                            }
                            return;

                            
                            
                        }
                        else
                        {
                            if (parseResult.id is ID reqID)
                            {
                                var response = JsonSerializer.SerializeUnsafe(ErrorResponse.MethodNotFound(reqID, parseResult.Method.ToString())).CopyToPooled();
                                ArrayPool<byte>.Shared.Return(message.Array!);
                                await ResponseAsync(response);
                            }
                            else
                            {
                                ArrayPool<byte>.Shared.Return(message.Array!);
                            }
                            return;
                        }
                    }
                case MessageKind.ResultResponse:
                    {
                        if (parseResult.id is ID id)
                        {
                            SetResult(id, parseResult.Result);
                        }
                        else
                        {
                            Debug.Fail("Result without id");
                        }
                        ArrayPool<byte>.Shared.Return(message.Array!);
                        return;

                    }
                case MessageKind.ErrorResponse:
                    {
                        SetError(parseResult.id, parseResult.Error);
                        ArrayPool<byte>.Shared.Return(message.Array!);
                        return;
                    }
                default:
                    {
                        Debug.Fail($"Unknown {nameof(MessageKind)}:{messageKind.ToString()}");
                        return;
                    }
            }
        }

        ConcurrentDictionary<long, RequestHandle> UnResponsedRequests { get; } = new ConcurrentDictionary<long, RequestHandle>();

        public ConcurrentBag<string> UnknownErrors { get; } = new ConcurrentBag<string>();

        public Channel<MessageHandle> MessageChannel { get; }
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
            var request = new Request<TParams>() { ID = new ID(id), Method = methodName, Params = parameters };
            var serializedRequest = JsonSerializer.SerializeUnsafe(request,formatterResolver).CopyToPooled();
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



}
