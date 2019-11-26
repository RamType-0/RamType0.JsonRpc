using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Utf8Json;
using System.Threading.Channels;
using System.Collections.Concurrent;
using System.Threading;

namespace RamType0.JsonRpc.Client
{
    class Connection
    {
        public bool ResolveResponse(ArraySegment<byte> segment)
        {
            var response = JsonSerializer.Deserialize<ResponseMessage>(segment.Array, segment.Offset, JsonResolver);
            if(response.ID is ID id)
            {
                //if(id.Number is long idNum)
                {
                    if (UnAnsweredRequests.TryGetValue(id, out var entry))
                    {
                        return entry.ResolveResponse(segment, JsonResolver);


                    }
                    else
                    {
                        return false;
                    }
                }
                
            }
            else
            {
                if (response.Error is ResponseError<object?> error)
                {
                    UnIdentifiableErrors.Add(error);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        ConcurrentDictionary<ID, RpcEntry> UnAnsweredRequests { get; } = new ConcurrentDictionary<ID, RpcEntry>();

        ConcurrentBag<ResponseError<object?>> UnIdentifiableErrors { get; } = new ConcurrentBag<ResponseError<object?>>();

        IJsonFormatterResolver JsonResolver { get; }
        Channel<ArraySegment<byte>> SerializedRequests { get; } = Channel.CreateUnbounded<ArraySegment<byte>>(new UnboundedChannelOptions() { SingleReader = true, SingleWriter = false });
        
        long id = 0;

        public ID GetUniqueID()
        {
            return new ID(Interlocked.Increment(ref id));
        }

    }
    abstract class RpcEntry
    {
        public abstract bool ResolveResponse(ArraySegment<byte> segment,IJsonFormatterResolver formatterResolver);
    }
    class RpcEntry<TDelegate, TParams, TResult, TDeserializer> : RpcEntry
    {
        sealed class RequestCompletionSource : IValueTaskSource
        {
            Connection Connection { get; }
            long segment;
            bool[] hasEnded = new bool[ushort.MaxValue + 1];

            long GetID(short token) => segment + (ushort)token;

            public void GetResult(short token)
            {
                throw new NotImplementedException();
            }

            public ValueTaskSourceStatus GetStatus(short token)
            {
                throw new NotImplementedException();
            }

            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                throw new NotImplementedException();
            }
        }

        ConcurrentDictionary<ID, (RequestCompletionSource completionSource, short token)> IdToToken { get; } = new ConcurrentDictionary<ID, (RequestCompletionSource completionSource, short token)>();

        //ConcurrentDictionary<ID, TResult> Results { get; } = new ConcurrentDictionary<ID, TResult>();
        //ConcurrentDictionary<ID, ResponseError<object?>> Errors { get; } = new ConcurrentDictionary<ID, ResponseError<object?>>();
        //ConcurrentDictionary<ID, (Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)> Continuations { get; } = new ConcurrentDictionary<ID, (Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)>();
        public override bool ResolveResponse(ArraySegment<byte> segment, IJsonFormatterResolver formatterResolver)
        {
            var response = JsonSerializer.Deserialize<Response<TResult>>(segment.Array!,segment.Offset, formatterResolver);
            
        }
    }
    class RpcReqEntry<TDelegate,TResult>
        where TDelegate : Delegate
    {
        private RpcReqEntry(Connection connection,string name)
        {
            Connection = connection;
            Name = name;
            var writer = new JsonWriter();
            writer.WriteString(name);
            var buffer = writer.GetBuffer();
            var array = new byte[headerSource.Length + buffer.Count];
            Buffer.BlockCopy(headerSource, 0, array, 0, headerSource.Length);
            Buffer.BlockCopy(buffer.Array!, buffer.Offset, array, headerSource.Length, buffer.Count);
            head = array;
            
        }
        static byte[] headerSource = new byte[] {(byte)'{',(byte)'"',(byte)'j',(byte)'s',(byte)'o',(byte)'n',(byte)'r',(byte)'p',(byte)'c',(byte)'"',(byte)':',(byte)'"',(byte)'2',(byte)'.',(byte)'0',(byte)'"',(byte)',',(byte)'"',(byte)'m',(byte)'e',(byte)'t',(byte)'h',(byte)'o',(byte)'d',(byte)'"',(byte)':',};
        static byte[] idHeader = JsonWriter.GetEncodedPropertyName("id");
        static byte[] paramsHeader = JsonWriter.GetEncodedPropertyName("params");
        public Connection Connection { get; }
        public string Name { get; }
        readonly byte[] head;
        
        void EnqueueSerializedRequest(ArraySegment<byte> serializedParams)
        {

        }


        void EnqueueSerializedNotification(ArraySegment<byte> serializedParams)
        {
            
        }

        void EnqueueSerializedNotification()
        {

        }

    }
}
