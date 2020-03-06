using System;

namespace RamType0.JsonRpc.Client
{
    using Protocol;
    public interface IResponseErrorHandler
    {
        Exception AsException<T>(ResponseError<T> error);
    }

}
