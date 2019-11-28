using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Client
{
    public class Client
    {
        public void ResolveResponse(ArraySegment<byte> segment)
        {
            var reader = new JsonReader(segment.Array!, segment.Offset);
            reader.ReadIsBeginObjectWithVerify();
            ID? _id = null;
            ResponseError<object?>? _error = null;
            ArraySegment<byte> resultSegment = default;

            while (true)
            {

                var nameSegment = reader.ReadPropertyNameSegmentRaw().AsSpan();
                if (nameSegment.SequenceEqual(stackalloc byte[] { (byte)'i', (byte)'d' }))
                {
                    _id = ID.Formatter.DeserializeNullableUnsafe(ref reader);
                }
                else if (nameSegment.SequenceEqual(stackalloc byte[] { (byte)'r', (byte)'e', (byte)'s', (byte)'u', (byte)'l', (byte)'t', }))
                {
                    resultSegment = reader.ReadNextBlockSegment();
                }
                else if (nameSegment.SequenceEqual(stackalloc byte[] { (byte)'e', (byte)'r', (byte)'r', (byte)'o', (byte)'r', }))
                {
                    _error = JsonResolver.GetFormatterWithVerify<ResponseError<object?>>().Deserialize(ref reader, JsonResolver);
                }


                if (!reader.ReadIsValueSeparator())
                {
                    break;
                }

            }

            if (_id is ID id)
            {
                if (UnResponsedRequests.TryRemove(id, out var req))
                {
                    try
                    {
                        if (_error is ResponseError<object?> error)
                        {
                            req.SetException(ErrorHandler.AsException(error));
                        }
                        else
                        {
                            req.SetResult(resultSegment, JsonResolver);
                        }
                    }
                    catch (Exception e)
                    {
                        req.SetException(e);
                    }
                }
                else
                {
                    throw new ArgumentException("ID conflicted!");
                }
            }
            else
            {
                if (_error is ResponseError<object?> error)
                {
                    UnIdentifiableErrors.Add(error);
                }
                else
                {
                    throw new JsonParsingException("Not a valid response.");
                }
            }
        }
        internal ConcurrentDictionary<ID, RequestCompletionSource> UnResponsedRequests { get; } = new ConcurrentDictionary<ID, RequestCompletionSource>();
        public ConcurrentBag<ResponseError<object?>> UnIdentifiableErrors { get; } = new ConcurrentBag<ResponseError<object?>>();
        public IJsonFormatterResolver JsonResolver { get; }
        public IRequestOutput Output { get; }
        public IResponseErrorHandler ErrorHandler { get; }
        long idSource = 0;

        public Client(IRequestOutput output, IJsonFormatterResolver jsonResolver,  IResponseErrorHandler errorHandler)
        {
            JsonResolver = jsonResolver;
            Output = output;
            ErrorHandler = errorHandler;
        }

        public Client(IRequestOutput output, IJsonFormatterResolver jsonResolver):this(output,jsonResolver,DefaultResponseErrorHandler.Instance) { }

        internal ID GetUniqueID()
        {
            return new ID(Interlocked.Increment(ref idSource));//1秒に40億件処理してもマイナス値になるまで60年以上かかるので問題ないはず・・・
        }

    }
    public abstract class RequestObjectSource
    {
        public Client Client { get; }
        public string MethodName { get; }
        public RequestObjectSource(Client client, string name)
        {
            Client = client;
            MethodName = name;
        }
        protected async ValueTask<TResult> RequestAsync<TParams, TResult>(TParams parameters)
            where TParams : IMethodParams

        {
            var request = new Request<TParams>() { ID = Client.GetUniqueID(), Method = MethodName, Params = parameters };
            var completion = RequestCompletionSource<TResult>.Get();
            Client.UnResponsedRequests.TryAdd(request.ID, completion);
            await Client.Output.SendRequestAsync(Client, request).ConfigureAwait(false);
            return await completion.ValueTask.ConfigureAwait(false);

        }
        protected async ValueTask RequestAsync<TParams>(TParams parameters)
            where TParams : IMethodParams

        {
            var request = new Request<TParams>() { ID = Client.GetUniqueID(), Method = MethodName, Params = parameters };
            var completion = RequestCompletionSource<NullResult>.Get();
            Client.UnResponsedRequests.TryAdd(request.ID, completion);
            await Client.Output.SendRequestAsync(Client, request).ConfigureAwait(false);
            await completion.VoidValueTask.ConfigureAwait(false);

        }
        protected TResult Request<TParams, TResult>(TParams parameters)
            where TParams : IMethodParams
        {
            return RequestAsync<TParams, TResult>(parameters).Result;
        }
        protected void Request<TParams>(TParams parameters)
            where TParams : IMethodParams
        {
            RequestAsync<TParams>(parameters).GetAwaiter().GetResult();//WAIT!!!!
        }
        protected void Notify<TParams>(TParams parameters)
            where TParams : IMethodParams
        {
            var notification = new Notification<TParams>() { Method = MethodName, Params = parameters };
            Client.Output.SendNotification(Client, notification);
        }
    }
    public sealed class Requester<TParams, TResult> : RequestObjectSource
        where TParams : IMethodParams
    {
        public Requester(Client client, string name) : base(client, name)
        {
        }
        public ValueTask<TResult> RequestAsync(TParams parameters)
        {
            return RequestAsync<TParams, TResult>(parameters);
        }
        public TResult Request(TParams parameters)
        {
            return Request<TParams, TResult>(parameters);
        }
    }
    public sealed class Requester<TParams> : RequestObjectSource
        where TParams : IMethodParams
    {
        public Requester(Client client, string name) : base(client, name)
        {
        }
        public ValueTask RequestAsync(TParams parameters)
        {
            return RequestAsync<TParams>(parameters);
        }
        public void Request(TParams parameters)
        {
            Request<TParams>(parameters);
        }
    }

    public sealed class Notifier<TParams> : RequestObjectSource
        where TParams : IMethodParams
    {
        public Notifier(Client client, string name) : base(client, name)
        {
        }
        public void Notify(TParams parameters)
        {
            base.Notify(parameters);
        }
    }

}
