using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Utf8Json;
using System.Threading.Tasks;
namespace RamType0.JsonRpc.Server
{
    /*
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

        async ValueTask Response<T>(T response) where T :  IResponseMessage
        {
            
            JsonSerializer.Serialize(Output, response);
            
        }

        void IResponseOutput.Response<T>(T response)
        {
            return Response(response);
        }
    }
    */
    /// <summary>
    /// 最終的な<see cref="IResponseMessage"/>の出力を行うクラスを示します。
    /// </summary>
    public interface IResponseOutput
    {
        protected ValueTask Response<T>(T response) where T :notnull, IResponseMessage;
        /// <summary>
        /// response.Resultが<see langword="null"/>の場合、非ジェネリックのResultResponseに明示的に変換する必要があることに注意してください。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public ValueTask ResponseResult<TResult>(ResultResponse<TResult> response)
        {
            if(response.Result == null)
            {
                return Response(ResultResponse.Create(response.ID));
            }
            else
            {
                return Response(response);
            }
        }

        public ValueTask ResponseResult(ResultResponse response)
        {
            return Response(response);
        }
        /// <summary>
        /// できる限り<see cref="ResponseException{TResponse, TError}(TResponse)"/>を利用してください。
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="response"></param>
        public ValueTask ResponseError<TResponse>(TResponse response) where TResponse :notnull, IErrorResponse
        {
            return Response(response);
        }
        /// <summary>
        /// <see cref="OperationCanceledException"/>など特殊な例外に対して別のエラーコードを割り振りたい場合にオーバーライドしてください。
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="response"></param>
        public ValueTask ResponseException<T>(T response)where T:notnull,IErrorResponse<Exception>
        {
            return ResponseError(response);
        }
    }
   
}
