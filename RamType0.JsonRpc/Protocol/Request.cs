using System;
using System.Runtime.Serialization;
using Utf8Json;

namespace RamType0.JsonRpc.Client
{
    using Protocol;
    public struct Request<T>
        //where T : notnull, IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp))]
        [DataMember(Name = "method")]
        public EscapedUTF8String Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
        [DataMember(Name = "id")]
        public ID ID { get; set; }
    }

    public struct Notification<T>
        //where T : notnull, IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp))]
        [DataMember(Name = "method")]
        public EscapedUTF8String Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
    }


    public sealed class DefaultResponseErrorHandler : IResponseErrorHandler
    {
        public static DefaultResponseErrorHandler Instance { get; } = new DefaultResponseErrorHandler();
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
            return FromPreDefinedError(error) ?? new ResponseErrorException<T>(error);
        }
    }
    [System.Serializable]
    public class ResponseErrorException<T> : System.IO.IOException
    {
        public ResponseError<T> Error { get; }
        public ResponseErrorException(ResponseError<T> error) : base(error.Message)
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
        public ServerNotInitializedException(ResponseError<T> error) : base(error.Message)
        {
            Error = error;
        }

    }


}