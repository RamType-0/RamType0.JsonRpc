using System;
using System.Runtime.Serialization;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc.Server
{
    public interface IResponseMessage : IMessage
    {
        [DataMember(Name ="id")]
        ID? ID { get; set; }
    }
    public interface IErrorResponse : IResponseMessage
    {
        [DataMember(Name ="error")]
        ErrorObject Error { get; set; }
    }

    public interface IErrorResponse<T> : IErrorResponse
        where T : notnull
    {
        [DataMember(Name ="error")]
        new ErrorObject<T> Error { get; set; }
        ErrorObject IErrorResponse.Error
        {
            get
            {
                return new ErrorObject(Error.Code, Error.Message);
            }

            set
            {
                var newError = Error;
                newError.Code = value.Code;
                newError.Message = value.Message;
                Error = newError;
            }
        }
    }

    interface IResultResponse<out T> : IResultResponse
    {
        [DataMember(Name ="result")]
        T Result { get; }
        //object? IResultResponse.result => result;
    }

    interface IResultResponse : IResponseMessage
    {
        //object? result { get; }
    }

    interface IErrorObject
    {
        [DataMember(Name ="code")]
        public long Code { get; set; }
        [DataMember(Name ="message")]
        public string Message { get; set; }
    }

    interface IErrorObject<out T> : IErrorObject
    {
        [DataMember(Name ="data")]
        T Data { get; }
    }

    /// <summary>
    /// 戻り値を持ったJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ResultResponse<T> : IResultResponse<T>
    {
        [DataMember(Name ="jsonrpc")]
        public JsonRpcVersion Version => default;
        [DataMember(Name = "result")]
        public T Result { get; set; }
        [DataMember(Name = "id")]
        public ID ID { get; set; }

        ID? IResponseMessage.ID { get => ID; set => ID = value ?? throw new InvalidOperationException(); }

        internal ResultResponse(ID id, T result) : this()
        {
            this.Result = result;
            this.ID = id;
        }
        
    }
    
    /// <summary>
    /// 戻り値を持たないJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    public struct ResultResponse : IResultResponse<ResultResponse.NullResult>
    {
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;

        /// <summary>
        /// nullとしてシリアライズさせるためのダミーです
        /// </summary>
        [DataMember(Name = "result")] 
        public NullResult Result => default;
        [DataMember(Name = "id")]
        public ID ID { get; set; }

        ID? IResponseMessage.ID { get => ID; set => ID = value ?? throw new InvalidOperationException(); }

        ResultResponse(ID id) : this()
        {
            this.ID = id;
        }
        public static ResultResponse<T> Create<T>(ID id, T result)
        {
            return new ResultResponse<T>(id, result);
        }
        public static ResultResponse Create(ID id)
        {
            return new ResultResponse(id);
        }
        [JsonFormatter(typeof(Formatter))]
        public readonly struct NullResult
        {
            public sealed class Formatter : IJsonFormatter<NullResult>
            {
                public NullResult Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
                {
                    if (!reader.ReadIsNull())
                    {
                        throw new JsonParsingException($"Expected null,but \"{Encoding.UTF8.GetString(reader.ReadNextBlockSegment())}\"");
                    }
                    return default;
                }

                public void Serialize(ref JsonWriter writer, NullResult value, IJsonFormatterResolver formatterResolver)
                {
                    writer.WriteNull();

                }
            }
        }
    }

    public struct ErrorResponse<T> : IErrorResponse<T>
        where T : notnull
    {
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        [DataMember(Name = "id")]
        public ID? ID { get; set; }
        [DataMember(Name ="error")]
        public ErrorObject<T> Error { get; set; }

        internal ErrorResponse(ID? id, ErrorObject<T> error) : this()
        {
            this.ID = id;
            this.Error = error;
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
        where T : notnull
    {
        [DataMember(Name ="code")]
        public long Code { get; set; }
        [DataMember(Name ="message")]
        public string Message { get; set; }
        [DataMember(Name ="data")]
        public T Data { get; set; }

        public ErrorObject(ErrorCode code, string message, T data)
        {
            this.Code = (long)code;
            this.Message = message;
            this.Data = data;
        }
        public ErrorObject(long errorCode, string message, T data) : this((ErrorCode)errorCode, message, data)
        {

        }
    }

    public struct ErrorResponse : IErrorResponse
    {
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        [DataMember(Name ="id")]
        public ID? ID { get; set; }
        [DataMember(Name ="error")]
        public ErrorObject Error { get; set; }

        public ErrorResponse(ID? id, ErrorObject error) : this()
        {
            this.ID = id;
            this.Error = error;
        }
        public static ErrorResponse<Exception> Exception(ID requestID, ErrorCode errorCode, Exception exception)
        {
            return new ErrorResponse<Exception>(requestID, ErrorObject.Exception(errorCode, exception));
        }

        public static ErrorResponse<Exception> Exception(ID requestID, long errorCode, Exception exception)
        {
            return new ErrorResponse<Exception>(requestID, ErrorObject.Exception(errorCode, exception));
        }
        public static ErrorResponse<Exception> ParseError(Exception e)
        {
            return new ErrorResponse<Exception>(null, ErrorObject.Exception(ErrorCode.ParseError, e));
        }
        public static ErrorResponse MethodNotFound(ID requestID, string methodName)
        {
            return new ErrorResponse(requestID, new ErrorObject(ErrorCode.MethodNotFound, $"The method you wanted to invoke not found. Assigned name:{methodName}"));
        }
        public static ErrorResponse InvalidParams(ID requestID, string paramsJson)
        {
            return new ErrorResponse(requestID, new ErrorObject(ErrorCode.InvalidParams, $"The params of request object was invalid. Assigned params:{paramsJson}"));
        }

        public static ErrorResponse InvalidRequest(string requestJson)
        {
            return new ErrorResponse(null, new ErrorObject(ErrorCode.InvalidRequest, $"The request object was invalid. Assigned request:{requestJson}"));
        }

    }
    public struct ErrorObject : IErrorObject
    {
        [DataMember(Name ="code")]
        public long Code { get; set; }
        [DataMember(Name ="message")]
        public string Message { get; set; }

        public ErrorObject(ErrorCode code, string message)
        {
            this.Code = (long)code;
            this.Message = message;
        }
        public ErrorObject(long errorCode, string message) : this((ErrorCode)errorCode, message)
        {

        }
        public static ErrorObject<Exception> Exception(ErrorCode errorCode, Exception exception)
        {
            return new ErrorObject<Exception>(errorCode, exception.Message, exception);
        }
        public static ErrorObject<Exception> Exception(long errorCode, Exception exception)
        {
            return Exception((ErrorCode)errorCode, exception);
        }

    }
}
