using Microsoft.Extensions.ObjectPool;
using RamType0.JsonRpc.Internal;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Resolvers;
namespace RamType0.JsonRpc
{
    public class RpcDomain
    {
        public RpcDomain() : this(Channel.CreateUnbounded<MessageHandle>(new UnboundedChannelOptions() { AllowSynchronousContinuations = true, SingleReader = true, SingleWriter = false })) { }
        public RpcDomain(Channel<MessageHandle> messageChannel) : this(messageChannel, StandardResolver.CamelCase) { }
        public RpcDomain(Channel<MessageHandle> messageChannel, IJsonFormatterResolver formatterResolver)
        {
            MessageChannel = messageChannel;
            JsonFormatterResolver = formatterResolver;
            var cancelMethod = RpcMethodEntry.ExplicitParams<CancelParams>(Cancell);
            MethodEntries.TryAdd(CancelParams.CancellationMethodName, cancelMethod);
        }
        long id;
        public IJsonFormatterResolver JsonFormatterResolver { get; set; }
        ConcurrentDictionary<long, RequestHandle> UnResponsedRequests { get; } = new ConcurrentDictionary<long, RequestHandle>();
        public ConcurrentBag<string> UnknownErrors { get; } = new ConcurrentBag<string>();
        public Channel<MessageHandle> MessageChannel { get; }
        ConcurrentDictionary<EscapedUTF8String, RpcAsyncMethodEntry> MethodEntries { get; } = new ConcurrentDictionary<EscapedUTF8String, RpcAsyncMethodEntry>();
        public bool AddMethod(string name, RpcAsyncMethodEntry methodEntry)
        {
            return MethodEntries.TryAdd(EscapedUTF8String.FromUnEscaped(name), methodEntry);
        }
        public bool RemoveMethod(string name, [NotNullWhen(true)]out RpcAsyncMethodEntry? methodEntry)
        {
            return MethodEntries.TryRemove(EscapedUTF8String.FromUnEscaped(name), out methodEntry);
        }

        /// <summary>
        /// メッセージを非同期で解決します。このメソッドはスレッドセーフであり、非同期での解決完了を待つことなく他のメッセージの解決を行うことができます。
        /// </summary>
        /// <param name="message"></param>
        /// <returns>メッセージの解決完了を表す<see cref="ValueTask"/>。この<see cref="ValueTask"/>の完了を待つことなく、他のメッセージの解決を開始することができます。</returns>
        async ValueTask ResolveMessageAsync(ArraySegment<byte> message)
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
                        await ResponseAsync(serializedResponse).ConfigureAwait(false);

                        return;
                    }
                case MessageKind.InvalidMessage:
                    {
                        var response = ErrorResponse.InvalidRequest(message);
                        var serializedResponse = JsonSerializer.SerializeUnsafe(response, JsonFormatterResolver).CopyToPooled();
                        ArrayPool<byte>.Shared.Return(message.Array!);
                        await ResponseAsync(serializedResponse).ConfigureAwait(false);
                        return;
                    }
                case MessageKind.ClientMessage:
                    {
                        if (MethodEntries.TryGetValue(parseResult.Method, out var entry))
                        {
                            var parameters = parseResult.Params;
                            var id = parseResult.id;
                            ArraySegment<byte> response;
                            try
                            {
                                response = await entry.ResolveRequestAsync(parameters, id).ConfigureAwait(false);
                            }
                            finally
                            {
                                if (id is ID reqID)
                                {
                                    if (PendingCancellableRequests.TryRemove(reqID, out var cts))
                                    {
                                        CTSPool.Return(cts);
                                    }
                                }
                                ArrayPool<byte>.Shared.Return(message.Array!);
                            }
                            if (response.Count != 0)
                            {
                                await ResponseAsync(response).ConfigureAwait(false);
                            }
                            return;
                        }
                        else
                        {
                            if (parseResult.id is ID reqID)
                            {
                                var response = JsonSerializer.SerializeUnsafe(ErrorResponse.MethodNotFound(reqID, parseResult.Method.ToString())).CopyToPooled();
                                ArrayPool<byte>.Shared.Return(message.Array!);
                                await ResponseAsync(response).ConfigureAwait(false);
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
        public async ValueTask ResolveMessagesAsync(IAsyncEnumerable<ArraySegment<byte>> messages,CancellationToken cancellationToken = default)
        {
            
            try
            {
                CallerDomain.Value = this;
                await foreach (var message in messages.WithCancellation(cancellationToken).ConfigureAwait(false))
                {
                    _ = ResolveMessageAsync(message);
                }
            }
            finally
            {
                CallerDomain.Value = null;
            }
            
        }
        public ValueTask RequestAsync<TParams>(EscapedUTF8String methodName, TParams parameters, IJsonFormatterResolver formatterResolver, IErrorHandler errorHandler,CancellationToken cancellationToken = default)
        {
            var handle = RequestAsyncCore<TParams, NullResult>(methodName, parameters, formatterResolver, errorHandler,cancellationToken);
            return handle.TaskVoid;
        }
        public ValueTask<TResult> RequestAsync<TParams, TResult>(EscapedUTF8String methodName, TParams parameters, IJsonFormatterResolver formatterResolver, IErrorHandler errorHandler,CancellationToken cancellationToken = default)
        {
            var handle = RequestAsyncCore<TParams, TResult>(methodName, parameters, formatterResolver, errorHandler,cancellationToken);
            return handle.Task;
        }
        private RequestHandle<TResult> RequestAsyncCore<TParams, TResult>(EscapedUTF8String methodName, TParams parameters, IJsonFormatterResolver formatterResolver, IErrorHandler errorHandler,CancellationToken cancellationToken = default)
        {
            var idNum = Interlocked.Increment(ref this.id);
            var id = new ID(idNum);
            if (cancellationToken.CanBeCanceled)
            {
                RegisterCancellation(id, cancellationToken);//ラムダ式のクロージャ生成を遅延
            }
            var request = new Request<TParams>() { ID = id, Method = methodName, Params = parameters };
            var serializedRequest = JsonSerializer.SerializeUnsafe(request, formatterResolver).CopyToPooled();
            var handle = RequestHandle<TResult>.Create(serializedRequest, errorHandler);
            if (!UnResponsedRequests.TryAdd(idNum, handle))
            {
                Debug.Fail("ID conflicted.");
            }
            _ = MessageChannel.Writer.WriteAsync(handle);

            

            return handle;
        }
        private void RegisterCancellation(ID id, CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => _ = NotifyAsync(CancelParams.CancellationMethodName, new CancelParams() { id = id }, StandardResolver.CamelCase));
        }
        public ValueTask NotifyAsync<TParams>(EscapedUTF8String methodName, TParams parameters, IJsonFormatterResolver formatterResolver)
        {
            var serializedNotification = JsonSerializer.SerializeUnsafe(new Notification<TParams>() { Method = methodName, Params = parameters }, formatterResolver).CopyToPooled();
            var handle = SendMessageHandle.Create(serializedNotification);
            _ = MessageChannel.Writer.WriteAsync(handle);
            return handle.Task;
        }
        public void SetResult(ID id, ArraySegment<byte> resultJson)
        {
            if (id.Number is long num && UnResponsedRequests.TryGetValue(num, out var handle))
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
            if (id is ID reqID && reqID.Number is long num && UnResponsedRequests.TryGetValue(num, out var handle))
            {
                handle.SetError(errorSegment);
            }
            else
            {
                var str = Encoding.UTF8.GetString(errorSegment);
                Debug.Write("Error with null id. content:"+str);
                UnknownErrors.Add(str);
            }
        }
        public ValueTask ResponseAsync(ArraySegment<byte> pooledSerializedResponse)
        {
            var handle = SendMessageHandle.Create(pooledSerializedResponse);
            _ = MessageChannel.Writer.WriteAsync(handle);
            return handle.Task;
        }
        #region Cancellation
        static AsyncLocal<RpcDomain?> CallerDomain { get; } = new AsyncLocal<RpcDomain?>();
        static DefaultObjectPool<CancellationTokenSource> CTSPool { get; } = new DefaultObjectPool<CancellationTokenSource>(new CancellationTokenSourcePoolPolicy());

        sealed class CancellationTokenSourcePoolPolicy : PooledObjectPolicy<CancellationTokenSource>
        {
            public override CancellationTokenSource Create()
            {
                return new CancellationTokenSource();
            }

            public override bool Return(CancellationTokenSource obj)
            {
                if (obj.IsCancellationRequested)
                {
                    obj.Dispose();
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
        ConcurrentDictionary<ID, CancellationTokenSource> PendingCancellableRequests { get; } = new ConcurrentDictionary<ID, CancellationTokenSource>();

        internal bool CancellPendingRequest(ID id)
        {
            if (PendingCancellableRequests.TryRemove(id,out var cts))
            {
                cts.Cancel();
                return true;
            }
            else
            {
                return false;
            }
        }

        internal void Cancell(CancelParams cancelParams) => CancellPendingRequest(cancelParams.id);

        CancellationToken AllocCancellationTokenInstance(ID id)
        {
            var cts = CTSPool.Get();
            if (!PendingCancellableRequests.TryAdd(id, cts))
            {
                CTSPool.Return(cts);
                ThrowAllocCancellationMultipleTimes();
            }
            return cts.Token;
            
        }
        [DoesNotReturn]
        static void ThrowAllocCancellationMultipleTimes()
        {
            throw new InvalidOperationException($"Cannot call {nameof(AllocCancellationToken)} multiple times with same id.");
        }

        internal static CancellationToken AllocCancellationToken(ID id)
        {
            var callerDomain = CallerDomain.Value;
            if(callerDomain is null)
            {
                ThrowCallerDomainIsNull();
            }
            return callerDomain.AllocCancellationTokenInstance(id);
            
        }

        [DoesNotReturn]
        private static void ThrowCallerDomainIsNull()
        {
            throw new InvalidOperationException("This method didnt called by request.");
        }

        #endregion

    }



}
