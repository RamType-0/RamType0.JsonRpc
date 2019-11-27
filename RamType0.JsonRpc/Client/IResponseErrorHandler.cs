using System;

namespace RamType0.JsonRpc.Client
{
    public interface IResponseErrorHandler
    {
        Exception AsException<T>(ResponseError<T> error);
    }

}
