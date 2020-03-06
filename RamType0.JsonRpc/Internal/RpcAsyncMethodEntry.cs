using RamType0.JsonRpc.Server;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    using Protocol;
    using Utf8Json.Resolvers;

    public abstract class RpcAsyncMethodEntry
    {
        protected RpcAsyncMethodEntry():this(StandardResolver.CamelCase,StandardResolver.ExcludeNullCamelCase)
        {

        }
        protected RpcAsyncMethodEntry(IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver)
        {
            ReadFormatterResolver = readFormatterResolver;
            WriteFormatterResolver = writeFormatterResolver;
        }

        public IJsonFormatterResolver ReadFormatterResolver { get; set; }
        public IJsonFormatterResolver WriteFormatterResolver { get; set; }
        public abstract ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> serializedParameters, ID? id);
    
    }
    sealed class RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntry
        where TMethod : notnull, IRpcAsyncMethodBody<TParams, TResult>
        where TDeserializer : notnull, IParamsDeserializer<TParams>
        where TModifier : notnull, IMethodParamsModifier<TParams>
    {

        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;

        public RpcAsyncMethodEntry(TMethod method, TDeserializer deserializer, TModifier modifier, IExceptionHandler exceptionHandler)
        {
            this.method = method;
            this.deserializer = deserializer;
            this.modifier = modifier;
            ExceptionHandler = exceptionHandler;
        }

        public IExceptionHandler ExceptionHandler { get; set; }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override sealed async ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> parametersSegment, ID? id)
        {

            TParams parameters;
            JsonReader reader;
            var array = parametersSegment.Array;
            if (array is null)
            {
                reader = new JsonReader(Array.Empty<byte>());
            }
            else
            {
                reader = new JsonReader(array, parametersSegment.Offset);
            }


            try
            {
                parameters = deserializer.Deserialize(ref reader, ReadFormatterResolver);
            }
            catch (JsonParsingException)
            {
                if (id is ID reqID)
                {
                    return JsonSerializer.SerializeUnsafe(ErrorResponse.InvalidParams(reqID, Encoding.UTF8.GetString(parametersSegment)), WriteFormatterResolver);
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }
            TResult result;
            try
            {
                modifier.Modify(ref parameters, parametersSegment, id, ReadFormatterResolver);
                result = await method.InvokeAsync(parameters);
            }
            catch (Exception e)
            {
                return ExceptionHandler.Handle(e, parametersSegment, id, ReadFormatterResolver, WriteFormatterResolver);

            }
            {
                if (id is ID requestID)
                {
                    if (result is null)
                    {
                        return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, new NullResult()), WriteFormatterResolver);
                    }
                    else
                    {
                        return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, result), WriteFormatterResolver);
                    }


                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }

        }
    }
    sealed class RpcAsyncMethodEntry<TMethod, TParams, TDeserializer, TModifier> : RpcAsyncMethodEntry
        where TMethod : notnull, IRpcAsyncMethodBody<TParams>
        where TDeserializer : notnull, IParamsDeserializer<TParams>
        where TModifier : notnull, IMethodParamsModifier<TParams>
    {

        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;

        public RpcAsyncMethodEntry(TMethod method, TDeserializer deserializer, TModifier modifier, IExceptionHandler exceptionHandler)
        {
            this.method = method;
            this.deserializer = deserializer;
            this.modifier = modifier;
            ExceptionHandler = exceptionHandler;
        }

        public IExceptionHandler ExceptionHandler { get; set; }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override sealed async ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> parametersSegment, ID? id)
        {

            TParams parameters;
            JsonReader reader;
            var array = parametersSegment.Array;
            if (array is null)
            {
                reader = new JsonReader(Array.Empty<byte>());
            }
            else
            {
                reader = new JsonReader(array, parametersSegment.Offset);
            }


            try
            {
                parameters = deserializer.Deserialize(ref reader, ReadFormatterResolver);
            }
            catch (JsonParsingException)
            {
                if (id is ID reqID)
                {
                    return JsonSerializer.SerializeUnsafe(ErrorResponse.InvalidParams(reqID, Encoding.UTF8.GetString(parametersSegment)), WriteFormatterResolver).CopyToPooled();
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }
            try
            {
                modifier.Modify(ref parameters, parametersSegment, id, ReadFormatterResolver);
                await method.InvokeAsync(parameters);
            }
            catch (Exception e)
            {
                return ExceptionHandler.Handle(e, parametersSegment, id, ReadFormatterResolver, WriteFormatterResolver).CopyToPooled();

            }
            {
                if (id is ID requestID)
                {
                    return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, new NullResult()), WriteFormatterResolver).CopyToPooled();
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }

        }
    }
}
