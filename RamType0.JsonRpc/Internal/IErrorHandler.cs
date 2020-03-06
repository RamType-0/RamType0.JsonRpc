using RamType0.JsonRpc.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc.Internal
{
    interface IErrorHandler
    {
        Exception AsException(ArraySegment<byte> errorResponse);
    }

    public sealed class DefaultErrorHandler : IErrorHandler
    {
        public static DefaultErrorHandler Instance { get; } = new DefaultErrorHandler();

        public Exception AsException(ArraySegment<byte> errorResponse)
        {
            var reader = new JsonReader(errorResponse.Array!, errorResponse.Offset);
            var error = JsonSerializer.Deserialize<ResponseError>(ref reader);
            switch ((ErrorCode)error.Code)
            {
                case ErrorCode.ParseError:
                    {
                        return new FormatException(error.Message);
                    }
                case ErrorCode.InvalidRequest:
                    {
                        return new FormatException(error.Message);
                    }
                case ErrorCode.MethodNotFound:
                    {
                        return new MissingMethodException(error.Message);
                    }
                case ErrorCode.InvalidParams:
                    {
                        return new ArgumentException(error.Message);
                    }
                default:
                    {
                        return new Exception(error.Message);
                    }
                case ErrorCode.ServerNotInitialized:
                    {
                        return new ServerNotInitializedException(error.Message);
                    }
                case ErrorCode.RequestCancelled:
                    {
                        return new OperationCanceledException(error.Message);
                    }
            }
        }
    }
}
