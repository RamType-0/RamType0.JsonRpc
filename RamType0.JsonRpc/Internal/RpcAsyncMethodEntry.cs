﻿using RamType0.JsonRpc.Server;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    public abstract class RpcAsyncMethodEntry
    {
        public abstract ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver);
        public ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readWriteFormatterResolver) => ResolveRequestAsync(serializedParameters, id, readWriteFormatterResolver, readWriteFormatterResolver);

    }
    class RpcAsyncMethodEntry<TMethod, TParams, TResult, TDeserializer, TModifier> : RpcAsyncMethodEntry
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
        public override sealed async ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> parametersSegment, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver)
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
                parameters = deserializer.Deserialize(ref reader, readFormatterResolver);
            }
            catch (JsonParsingException)
            {
                if (id is ID reqID)
                {
                    return JsonSerializer.SerializeUnsafe(ErrorResponse.InvalidParams(reqID, Encoding.UTF8.GetString(parametersSegment)), writeFormatterResolver);
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }
            TResult result;
            try
            {
                modifier.Modify(ref parameters, parametersSegment, id, readFormatterResolver);
                result = await method.InvokeAsync(parameters);
            }
            catch (Exception e)
            {
                return ExceptionHandler.Handle(e, parametersSegment, id, readFormatterResolver, writeFormatterResolver);

            }
            {
                if (id is ID requestID)
                {
                    if (result is null)
                    {
                        return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, new NullResult()), writeFormatterResolver);
                    }
                    else
                    {
                        return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, result), writeFormatterResolver);
                    }


                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }

        }
    }
    class RpcAsyncMethodEntry<TMethod, TParams, TDeserializer, TModifier> : RpcAsyncMethodEntry
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
        public override sealed async ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> parametersSegment, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver)
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
                parameters = deserializer.Deserialize(ref reader, readFormatterResolver);
            }
            catch (JsonParsingException)
            {
                if (id is ID reqID)
                {
                    return JsonSerializer.SerializeUnsafe(ErrorResponse.InvalidParams(reqID, Encoding.UTF8.GetString(parametersSegment)), writeFormatterResolver);
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }
            try
            {
                modifier.Modify(ref parameters, parametersSegment, id, readFormatterResolver);
                await method.InvokeAsync(parameters);
            }
            catch (Exception e)
            {
                return ExceptionHandler.Handle(e, parametersSegment, id, readFormatterResolver, writeFormatterResolver);

            }
            {
                if (id is ID requestID)
                {
                    return JsonSerializer.SerializeUnsafe(ResultResponse.Create(requestID, new NullResult()), writeFormatterResolver);
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }

            }

        }
    }
}
