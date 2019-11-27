using RamType0.JsonRpc.Server;
using System;
using System.Runtime.Serialization;
using System.Threading.Tasks.Sources;
using Utf8Json;

namespace RamType0.JsonRpc.Client
{
    public struct Request<T>
        where T:notnull,IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp.Nullable))]
        [DataMember(Name = "method")]
        public string Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
        [DataMember(Name = "id")]
        public ID ID { get; set; }
    }

    public struct Notification<T>
        where T : notnull, IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp.Nullable))]
        [DataMember(Name = "method")]
        public string Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
    }


    /// <summary>
    /// レスポンスのデシリアライズ用の構造体。レスポンスのフォーマットが不正のときに関する仕様がないため、特にハンドリングしていない・・・
    /// </summary>
    public struct ResponseMessage
    {
        //[DataMember(Name = "jsonrpc")]
        //public JsonRpcVersion Version { get; set; }
        //[DataMember(Name = "result")]
        //public object? Result { get; set; }
        [DataMember(Name = "id")]
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID? ID { get; set; }
        //[DataMember(Name = "error")]
        //public ResponseError<object?>? Error { get; set; }
    }

    public struct ErrorResponseMessage
    {
        [DataMember(Name = "error")]
        public ResponseError<object?>? Error { get; set; }
    }

    public struct Response<T>
    {
        [DataMember(Name = "result")]
        public T Result { get; set; }
        [DataMember(Name = "id")]
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID ID { get; set; }
        [DataMember(Name = "error")]
        public ResponseError<object?>? Error { get; set; }
    }



    public sealed class DefaultResponseErrorHandler : IResponseErrorHandler
    {
        public static Exception? FromPreDefinedError<T>(ResponseError<T> error)
        {
            string message = error.Message;

            switch ((ErrorCode)error.Code)
            {
                case ErrorCode.ParseError:
                case ErrorCode.InvalidRequest:
                    return new RequestParsingException<T>(error);
                case ErrorCode.MethodNotFound:
                    return new MissingMethodException(message);
                case ErrorCode.InvalidParams:
                    return new ArgumentException(message);
                case ErrorCode.InternalError:
                    return new ServerInternalErrorException<T>(error);
                case ErrorCode.ServerNotInitialized:
                    return new ServerNotInitializedException<T>(error);
                default:
                    return null;
            }
            
        }

        public Exception AsException<T>(ResponseError<T> error)
        {
            return FromPreDefinedError(error)?? new ResponseErrorException<T>(error);
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

    }

        [System.Serializable]
    public class ResponseErrorException<T> : System.IO.IOException
    {
        public ResponseError<T> Error { get; }
        public ResponseErrorException(ResponseError<T> error) :base(error.Message)
        {
            Error = error;
        }
        
    }

        public class RequestParsingException<T> : ResponseErrorException<T>
        {
            public RequestParsingException(ResponseError<T> error) : base(error)
            {
            }
        }



        [Serializable]
        public class ServerInternalErrorException<T> : ResponseErrorException<T>
        {
            public ServerInternalErrorException(ResponseError<T> error) : base(error)
            {
            }
        }


    [Serializable]
    public class ServerNotInitializedException<T> : InvalidOperationException
    {
        public ResponseError<T> Error { get; }
        public ServerNotInitializedException(ResponseError<T> error):base(error.Message)
        {
            Error = error;
        }
        
    }

        [System.Serializable]
    public class RequestCancelledException<T> : System.OperationCanceledException
    {
        public ResponseError<T> Error { get; }
        public RequestCancelledException(ResponseError<T> error) : base(error.Message) 
        {
            Error = error;
        }

    }

    
}