using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utf8Json;
namespace RamType0.JsonRpc
{
    /// <summary>
    /// スレッドセーフ、標準的な<see cref="IResponser"/>の実装です。
    /// </summary>
    public class DefaultResponser : IResponser
    {
        public DefaultResponser(Stream output)
        {
            Output = output;
        }

        Stream Output { get; }
        public object OutputWritingLocker { get; } = new object();

        public void Response<T>(T response) where T : struct, IResponse
        {
            lock (OutputWritingLocker)
            {
                JsonSerializer.Serialize(Output, response);
            }
        }
    }

    /// <summary>
    /// <see cref="Response{T}(T)"/>を様々な形で実装することで、ヘッダーの付与などがサポートされます。
    /// </summary>
    public interface IResponser
    {
        void Response<T>(T response) where T : struct, IResponse;
    }

  
    public interface IResponse
    {

    }
    interface IErrorResponse : IResponse
    {

    }

    interface IResultResponse :IResponse{ }
    /// <summary>
    /// 戻り値を持ったJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct ResultResponse<T> : IResultResponse
    {
        public readonly JsonRpcVersion jsonrpc;
        public readonly T result;
        public readonly ID id;

        internal ResultResponse(ID id, T result) : this()
        {
            this.result = result;
            this.id = id;
        }
    }
    /// <summary>
    /// 戻り値を持たないJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    public readonly struct ResultResponse : IResultResponse
    {
        public readonly JsonRpcVersion jsonrpc;
        public readonly object? result;
        public readonly ID id;

        ResultResponse(ID id) : this()
        {
            this.id = id;
        }
        public static ResultResponse<T> Result<T>(ID id,T result)
        {
            return new ResultResponse<T>(id, result);
        }
        public static ResultResponse Result(ID id)
        {
            return new ResultResponse(id);
        }
    }

    public readonly struct ErrorResponse<T> : IErrorResponse
        where T:notnull
    {
        public readonly JsonRpcVersion jsonrpc;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public readonly ID? id;
        public readonly ErrorObject<T> error;

        internal ErrorResponse(ID? id, ErrorObject<T> error) : this()
        {
            this.id = id;
            this.error = error;
        }
    }
    public enum ErrorCode : long
    {
        // Defined by JSON RPC
        ParseError = -32700,
        InvalidRequest = -32600,
        MethodNotFound = -32601,
        InvalidParams = -32602,
        InternalError = -32603,
        serverErrorStart = -32099,
        serverErrorEnd = -32000,
        ServerNotInitialized = -32002,
        UnknownErrorCode = -32001,

        // Defined by the language server protocol.
        //RequestCancelled = -32800,
        //ContentModified = -32801,
    }
    public readonly struct ErrorObject<T>
        where T: notnull
    {
        public readonly long code;
        public readonly string message;
        public readonly T data;

        public ErrorObject(ErrorCode code, string message, T data)
        {
            this.code = (long)code;
            this.message = message;
            this.data = data;
        }
        public ErrorObject(long errorCode, string message,T data) : this((ErrorCode)errorCode, message,data)
        {

        }
    }

    public readonly struct ErrorResponse : IErrorResponse
    {
        public readonly JsonRpcVersion jsonrpc;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public readonly ID? id;
        public readonly ErrorObject error;

        public ErrorResponse(ID? id, ErrorObject error) : this()
        {
            this.id = id;
            this.error = error;
        }
        public static ErrorResponse<Exception> Exception(ID requestID,ErrorCode errorCode, Exception exception)
        {
            return new ErrorResponse<Exception>(requestID, ErrorObject.Exception(errorCode,exception));
        }

        public static ErrorResponse<Exception> Exception(ID requestID, long errorCode, Exception exception)
        {
            return new ErrorResponse<Exception>(requestID, ErrorObject.Exception(errorCode, exception));
        }
        public static ErrorResponse<Exception> ParseError(Exception e)
        {
            return new ErrorResponse<Exception>(null, ErrorObject.Exception(ErrorCode.ParseError, e));
        }
        public static ErrorResponse MethodNotFound(ID requestID,string methodName)
        {
            return new ErrorResponse(requestID, new ErrorObject(ErrorCode.MethodNotFound,$"The method you wanted to invoke not found. Assigned name:{methodName}"));
        }
        public static ErrorResponse InvalidParams(ID requestID,string paramsJson)
        {
            return new ErrorResponse(null, new ErrorObject(ErrorCode.InvalidParams, $"The params of request object was invalid. Assigned params:{paramsJson}"));
        }

        public static ErrorResponse InvalidRequest(string requestJson)
        {
            return new ErrorResponse(null, new ErrorObject(ErrorCode.InvalidRequest, $"The request object was invalid. Assigned request:{requestJson}"));
        }

    }
    public readonly struct ErrorObject
    {
        public readonly long code;
        public readonly string message;

        public ErrorObject(ErrorCode code, string message)
        {
            this.code = (long)code;
            this.message = message;
        }
        public ErrorObject(long errorCode, string message):this((ErrorCode)errorCode,message)
        {
            
        }
        public static ErrorObject<Exception> Exception(ErrorCode errorCode,Exception exception)
        {
            return new ErrorObject<Exception>(errorCode, exception.Message, exception);
        }
        public static ErrorObject<Exception> Exception(long errorCode, Exception exception)
        {
            return Exception((ErrorCode)errorCode, exception);
        }

    }
}
