using System;
using Utf8Json;

namespace RamType0.JsonRpc
{

    public interface IExceptionHandler
    {
        public ArraySegment<byte> Handle(Exception e, ArraySegment<byte> parametersJson, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver);
    }

    public static class ExceptionHandler
    {
        public static ArraySegment<byte> HandleStandard(Exception e, ArraySegment<byte> parametersJson, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatResolver)
        {
            if (e is RpcExplicitResponseException rpcError)
            {
                return rpcError.Response(parametersJson, id, readFormatterResolver, writeFormatResolver);
            }
            else
            {
                if (id is ID reqID)
                {
                    if(e is OperationCanceledException oce)
                    {
                        return JsonSerializer.SerializeUnsafe(new ErrorResponse(reqID, new ResponseError(ErrorCode.RequestCancelled, oce.Message)), writeFormatResolver);
                    }
                    var errorResponse = e switch
                    {
                        ArgumentException ae => ErrorResponse.Exception(reqID, ErrorCode.InvalidParams, ae),
                        ServerNotInitializedException sne => ErrorResponse.Exception(reqID, ErrorCode.ServerNotInitialized, sne),
                        _ => ErrorResponse.Exception(reqID, ErrorCode.InternalError, e),


                    };
                    return JsonSerializer.SerializeUnsafe(errorResponse, writeFormatResolver);
                }
                else
                {
                    return ArraySegment<byte>.Empty;
                }
            }


        }


    }
    public sealed class StandardExceptionHandler : IExceptionHandler
    {
        public static StandardExceptionHandler Instance { get; } = new StandardExceptionHandler();
        public ArraySegment<byte> Handle(Exception e, ArraySegment<byte> parametersJson, ID? id, IJsonFormatterResolver readFormatterResolver, IJsonFormatterResolver writeFormatterResolver)
        {
            return ExceptionHandler.HandleStandard(e, parametersJson, id, readFormatterResolver, writeFormatterResolver);
        }
    }
}
