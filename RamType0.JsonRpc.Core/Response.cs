using System;
using System.Runtime.Serialization;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc
{



    /// <summary>
    /// 戻り値を持ったJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ResultResponse<T>
    {
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [DataMember(Name = "result")]
        public T Result { get; set; }
        [DataMember(Name = "id")]
        public ID ID { get; set; }

        internal ResultResponse(ID id, T result) : this()
        {
            this.Result = result;
            this.ID = id;
        }

    }

    /// <summary>
    /// 戻り値を持たないJsonRpcメソッドが正常に完了した際の応答を示します。
    /// </summary>
    public struct ResultResponse
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
    }

    public struct ErrorResponse<T>
    //where T : notnull
    {
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        [DataMember(Name = "id")]
        public ID? ID { get; set; }
        [DataMember(Name = "error")]
        public ResponseError<T> Error { get; set; }

        public ErrorResponse(ID? id, ResponseError<T> error) : this()
        {
            this.ID = id;
            this.Error = error;
        }
    }
    public struct ResponseError<T>
    {
        [DataMember(Name = "code")]
        public long Code { get; set; }
        [DataMember(Name = "message")]
        public string Message { get; set; }
        [DataMember(Name = "data")]
        public T Data { get; set; }

        public ResponseError(ErrorCode code, string message, T data)
        {
            this.Code = (long)code;
            this.Message = message;
            this.Data = data;
        }
        public ResponseError(long errorCode, string message, T data) : this((ErrorCode)errorCode, message, data)
        {

        }
    }

    public struct ErrorResponse
    {
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        [DataMember(Name = "id")]
        public ID? ID { get; set; }
        [DataMember(Name = "error")]
        public ResponseError Error { get; set; }

        public ErrorResponse(ID? id, ResponseError error) : this()
        {
            this.ID = id;
            this.Error = error;
        }
        public static ErrorResponse<Exception> Exception(ID requestID, ErrorCode errorCode, Exception exception)
        {
            return new ErrorResponse<Exception>(requestID, ResponseError.Exception(errorCode, exception));
        }

        public static ErrorResponse<Exception> Exception(ID requestID, long errorCode, Exception exception)
        {
            return new ErrorResponse<Exception>(requestID, ResponseError.Exception(errorCode, exception));
        }
        public static ErrorResponse<Exception> ParseError(Exception e)
        {
            return new ErrorResponse<Exception>(null, ResponseError.Exception(ErrorCode.ParseError, e));
        }

        public static ErrorResponse ParseError(ArraySegment<byte> json)
        {
            return new ErrorResponse(null, new ResponseError(ErrorCode.ParseError,  "Invalid syntax json. Assigned Json:" +Encoding.UTF8.GetString(json)));
        }
        public static ErrorResponse MethodNotFound(ID requestID, string methodName)
        {
            return new ErrorResponse(requestID, new ResponseError(ErrorCode.MethodNotFound, $"The method you wanted to invoke not found. Assigned name:{methodName}"));
        }
        public static ErrorResponse InvalidParams(ID requestID, string paramsJson)
        {
            return new ErrorResponse(requestID, new ResponseError(ErrorCode.InvalidParams, $"The params of request object was invalid. Assigned params:{paramsJson}"));
        }


        public static ErrorResponse InvalidRequest(string requestJson)
        {
            return new ErrorResponse(null, new ResponseError(ErrorCode.InvalidRequest, $"The request object was invalid. Assigned request:{requestJson}"));
        }

        public static ErrorResponse InvalidRequest(ArraySegment<byte> json)
        {
            return InvalidRequest(Encoding.UTF8.GetString(json));
        }

    }
    public struct ResponseError
    {
        [DataMember(Name = "code")]
        public long Code { get; set; }
        [DataMember(Name = "message")]
        public string Message { get; set; }

        public ResponseError(ErrorCode code, string message)
        {
            this.Code = (long)code;
            this.Message = message;
        }
        public ResponseError(long errorCode, string message) : this((ErrorCode)errorCode, message)
        {

        }
        public static ResponseError<Exception> Exception(ErrorCode errorCode, Exception exception)
        {
            return new ResponseError<Exception>(errorCode, exception.Message, exception);
        }
        public static ResponseError<Exception> Exception(long errorCode, Exception exception)
        {
            return Exception((ErrorCode)errorCode, exception);
        }

    }
}
