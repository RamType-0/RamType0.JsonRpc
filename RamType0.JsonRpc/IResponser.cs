using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utf8Json;
namespace RamType0.JsonRpc
{
    /// <summary>
    /// スレッドセーフ、標準的な<see cref="IResponseOutput"/>の実装です。
    /// </summary>
    public class DefaultResponseOutput : IResponseOutput
    {
        public DefaultResponseOutput(Stream output)
        {
            Output = Stream.Synchronized(output);
        }

        Stream Output { get; }

        void Response<T>(T response) where T :  IResponseMessage
        {
            
            JsonSerializer.Serialize(Output, response);
            
        }

        void IResponseOutput.Response<T>(T response)
        {
            Response(response);
        }
    }

    /// <summary>
    /// 最終的な<see cref="IResponseMessage"/>の出力を行うクラスを示します。
    /// </summary>
    public interface IResponseOutput
    {
        protected void Response<T>(T response) where T :notnull, IResponseMessage;
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
        protected internal void ResponseError<TResponse>(TResponse response) where TResponse :notnull, IErrorResponse
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
   
}
