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

        void Response<T>(T response) where T :  IResponseMessage
        {
            lock (OutputWritingLocker)
            {
                JsonSerializer.Serialize(Output, response);
            }
        }

        void IResponser.Response<T>(T response)
        {
            Response(response);
        }
    }

    /// <summary>
    /// 最終的な<see cref="IResponseMessage"/>の出力を行うクラスを示します。
    /// </summary>
    public interface IResponser
    {
        protected void Response<T>(T response) where T : IResponseMessage;
        public void ResponseResult<TResult>(ResultResponse<TResult> response)
        {
            Response(response);
        }

        public void ResponseResult(ResultResponse response)
        {
            Response(response);
        }
        /// <summary>
        /// できる限り<see cref="ResponseException{TResponse, TError}(TResponse)"/>を利用してください。
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="response"></param>
        protected internal void ResponseError<TResponse>(TResponse response) where TResponse : IErrorResponse
        {
            Response(response);
        }
        /// <summary>
        /// <see cref="OperationCanceledException"/>など特殊な例外に対して別のエラーコードを割り振りたい場合にオーバーライドしてください。
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="response"></param>
        public void ResponseException<T>(T response)where T:IErrorResponse<Exception>
        {
            Response(response);
        }
    }


    /// <summary>
    /// iMessageはApple Inc.の登録商標です
    /// </summary>
    public interface IMessage
    {
        JsonRpcVersion jsonrpc { get; }
    }
    public interface IResponseMessage : IMessage
    {
        ID? id { get; set; }
    }
    public interface IErrorResponse : IResponseMessage
    {
        ErrorObject error { get; set; }
    }

    public interface IErrorResponse<T> :IErrorResponse
        where T : notnull
    {
        new ErrorObject<T> error { get; set; }
        ErrorObject IErrorResponse.error
        {
            get
            {
                return new ErrorObject(error.code, error.message);
            }

            set
            {
                var newError = error;
                newError.code = value.code;
                newError.message = value.message;
                error = newError;
            }
        }
    }

    interface IResultResponse<out T> :IResultResponse
    {
        T result { get; }
        //object? IResultResponse.result => result;
    }

    interface IResultResponse : IResponseMessage
    {
        //object? result { get; }
    }

    interface IErrorObject
    {
        public long code { get; set; }
        public string message { get; set; }
    }

    interface IErrorObject<out T> : IErrorObject
    {
        T data { get; }
    }

    /// <summary>
    /// 戻り値を持ったJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ResultResponse<T> : IResultResponse<T>
    {
        public JsonRpcVersion jsonrpc => default;
        public T result { get; set; }
        public ID id { get; set; }

        ID? IResponseMessage.id { get => id; set => id = value ?? throw new InvalidOperationException(); }

        internal ResultResponse(ID id, T result) : this()
        {
            this.result = result;
            this.id = id;
        }
    }
    /// <summary>
    /// 戻り値を持たないJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    public struct ResultResponse : IResultResponse<ResultResponse.VoidMethodResult>
    {
        public JsonRpcVersion jsonrpc => default;
        /// <summary>
        /// nullとしてシリアライズさせるためのダミーです
        /// </summary>
        public VoidMethodResult result => default;
        public ID id { get; set; }

        ID? IResponseMessage.id { get => id; set => id = value ?? throw new InvalidOperationException(); }

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
        [JsonFormatter(typeof(Formatter))]
        public readonly struct VoidMethodResult
        {
            public sealed class Formatter : IJsonFormatter<VoidMethodResult>
            {
                public VoidMethodResult Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    if (!reader.ReadIsNull())
                    {
                        throw new JsonParsingException($"Expected null,but \"{Encoding.UTF8.GetString(reader.ReadNextBlockSegment())}\"");
                    }
                    return default;
                }

                public void Serialize(ref JsonWriter writer, VoidMethodResult value, IJsonFormatterResolver formatterResolver)
                {
                    writer.WriteNull();
                    
                }
            }
        }
    }

    public struct ErrorResponse<T> : IErrorResponse<T>
        where T:notnull
    {
        public JsonRpcVersion jsonrpc => default;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID? id { get; set; }
        public ErrorObject<T> error { get; set; }

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
    public struct ErrorObject<T> : IErrorObject<T>
        where T: notnull
    {
        public long code { get; set; }
        public string message { get; set; }
        public T data { get; set; }

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

    public struct ErrorResponse : IErrorResponse
    {
        public JsonRpcVersion jsonrpc => default;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID? id { get; set; }
        public ErrorObject error { get; set; }

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
    public struct ErrorObject : IErrorObject
    {
        public long code { get; set; }
        public string message { get; set; }

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
