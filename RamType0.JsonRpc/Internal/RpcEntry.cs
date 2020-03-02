using RamType0.JsonRpc.Server;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using Utf8Json;
namespace RamType0.JsonRpc.Internal
{
    public abstract class RpcEntry : IRpcEntry
    {
        public static RpcEntry FromDelegate<T>(T d)
            where T:notnull, Delegate
        {
            if (d is MulticastDelegate multicast && multicast.GetInvocationList().Length > 1)
            {
                return RpcDelegateEntryFactory<T>.Instance.CreateEntry(d);
            }
            else if (d.Method.IsStatic)
            {
                return RpcStaticMethodEntryFactory<T>.Instance.CreateEntry(d);
            }
            else
            {
                return RpcInstanceMethodEntryFactory<T>.Instance.CreateEntry(d);
            }
            
        }

        public static RpcEntry ExplicitParams<TParams>(ExplicitParamsAction<TParams> explicitParamsAction)
        {
            return RpcExplicitParamsActionDelegateEntryFactory<TParams>.Instance.CreateEntry(explicitParamsAction);
        }

        public static RpcEntry ExplicitParams<TParams,TResult>(ExplicitParamsFunc<TParams,TResult> explicitParamsFunc)
        {
            return RpcExplicitParamsFuncDelegateEntryFactory<TParams, TResult>.Instance.CreateEntry(explicitParamsFunc);
        }

        public abstract ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readFormatterResolver,IJsonFormatterResolver writeFormatterResolver);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readWriteFormatterResolver) => ResolveRequest(serializedParameters, id, readWriteFormatterResolver, readWriteFormatterResolver);

    }
    public class RpcEntry<TMethod,TParams,TResult,TDeserializer,TModifier> : RpcEntry
        where TMethod : notnull,IRpcMethodBody<TParams, TResult>
        where TParams : notnull
        where TDeserializer : notnull,IParamsDeserializer<TParams>
        where TModifier : notnull,IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;

        public RpcEntry(TMethod method, TDeserializer deserializer, TModifier modifier)
        {
            this.method = method;
            this.deserializer = deserializer;
            this.modifier = modifier;
        }
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public override ArraySegment<byte> ResolveRequest(ArraySegment<byte> parametersSegment, ID? id,IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver)
        {

            TParams parameters;
            JsonReader reader;
            var array = parametersSegment.Array;
            if(array is null)
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
                modifier.Modify(ref parameters,parametersSegment,id,readFormatterResolver);
                result = method.Invoke(parameters);
            }
            catch(RpcErrorException rpcError)
            {
                return rpcError.Response(parametersSegment, id, readFormatterResolver, writeFormatterResolver);
            }
            catch(Exception e)
            {
                if(id is ID requestID)
                {
                    return JsonSerializer.SerializeUnsafe(ErrorResponse.Exception(requestID, ErrorCode.InternalError, e),writeFormatterResolver);
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }
                
            }
            {
                if (id is ID requestID)
                {
                    if(result is null)
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

   
}
