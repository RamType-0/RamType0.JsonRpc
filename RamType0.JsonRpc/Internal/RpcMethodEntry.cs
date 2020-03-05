using RamType0.JsonRpc.Server;
using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
namespace RamType0.JsonRpc.Internal
{
    public abstract class RpcMethodEntry : RpcAsyncMethodEntry
    {
        public static RpcAsyncMethodEntry FromDelegate<T>(T d) where T : notnull, Delegate => FromDelegate(d, StandardExceptionHandler.Instance);
        public static RpcAsyncMethodEntry FromDelegate<T>(T d,IExceptionHandler exceptionHandler)
            where T:notnull, Delegate
        {
            if (d is MulticastDelegate multicast && multicast.GetInvocationList().Length > 1)
            {
                return RpcDelegateEntryFactory<T>.Instance.CreateAsyncMethodEntry(d,exceptionHandler);
            }
            else if (d.Method.IsStatic)
            {
                return RpcStaticMethodEntryFactory<T>.Instance.CreateAsyncMethodEntry(d,exceptionHandler);
            }
            else
            {
                return RpcInstanceMethodEntryFactory<T>.Instance.CreateAsyncMethodEntry(d,exceptionHandler);
            }
            
        }

        public static RpcAsyncMethodEntry ExplicitParams<TParams>(Action<TParams> explicitParamsAction) => ExplicitParams(explicitParamsAction, StandardExceptionHandler.Instance);

        public static RpcAsyncMethodEntry ExplicitParams<TParams>(Action<TParams> explicitParamsAction, IExceptionHandler exceptionHandler)
        {
            return RpcExplicitParamsActionDelegateEntryFactory<TParams>.Instance.CreateAsyncMethodEntry(explicitParamsAction,exceptionHandler);
        }

        public static RpcAsyncMethodEntry ExplicitParams<TParams, TResult>(Func<TParams, TResult> explicitParamsFunc) => ExplicitParams(explicitParamsFunc, StandardExceptionHandler.Instance);

        public static RpcAsyncMethodEntry ExplicitParams<TParams,TResult>(Func<TParams,TResult> explicitParamsFunc, IExceptionHandler exceptionHandler)
        {
            return RpcExplicitParamsFuncDelegateEntryFactory<TParams, TResult>.Instance.CreateAsyncMethodEntry(explicitParamsFunc,exceptionHandler);
        }

        public abstract ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readFormatterResolver,IJsonFormatterResolver writeFormatterResolver);
        public override sealed ValueTask<ArraySegment<byte>> ResolveRequestAsync(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver)
        {
            return new ValueTask<ArraySegment<byte>>(ResolveRequest(serializedParameters, id, readFormatterResolver, writeFormatterResolver));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArraySegment<byte> ResolveRequest(ArraySegment<byte> serializedParameters, ID? id, IJsonFormatterResolver readWriteFormatterResolver) => ResolveRequest(serializedParameters, id, readWriteFormatterResolver, readWriteFormatterResolver);
    }
    public class RpcMethodEntry<TMethod,TParams,TResult,TDeserializer,TModifier> : RpcMethodEntry
        where TMethod : notnull,IRpcMethodBody<TParams, TResult>
        where TDeserializer : notnull,IParamsDeserializer<TParams>
        where TModifier : notnull,IMethodParamsModifier<TParams>
    {
        internal TMethod method;
        internal TDeserializer deserializer;
        internal TModifier modifier;

        public IExceptionHandler ExceptionHandler { get; set; }

        public RpcMethodEntry(TMethod method, TDeserializer deserializer, TModifier modifier,IExceptionHandler exceptionHandler)
        {
            this.method = method;
            this.deserializer = deserializer;
            this.modifier = modifier;
            ExceptionHandler = exceptionHandler;
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
            catch(Exception e)
            {
                return ExceptionHandler.Handle(e, parametersSegment, id, readFormatterResolver, writeFormatterResolver);
                
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
