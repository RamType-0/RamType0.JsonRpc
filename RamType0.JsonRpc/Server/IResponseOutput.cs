using System;
using System.Threading.Tasks;
namespace RamType0.JsonRpc.Server
{

    /// <summary>
    /// 最終的な<see cref="IResponseMessage"/>の出力を行うクラスを示します。
    /// </summary>
    public interface IResponseOutput
    {
        protected ValueTask ResponseAsync<T>(Server server,T response) where T : notnull, IResponseMessage;
        /// <summary>
        /// response.Resultが<see langword="null"/>の場合、非ジェネリックのResultResponseに明示的に変換する必要があることに注意してください。
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public ValueTask ResponseResult<TResult>(Server server, ResultResponse<TResult> response)
        {
            if (response.Result == null)
            {
                return ResponseAsync(server, ResultResponse.Create(response.ID));
            }
            else
            {
                return ResponseAsync(server,response);
            }
        }

        public ValueTask ResponseResult(Server server, ResultResponse response)
        {
            return ResponseAsync(server,response);
        }
        /// <summary>
        /// できる限り<see cref="ResponseException{TResponse, TError}(TResponse)"/>を利用してください。
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="response"></param>
        public ValueTask ResponseError<TResponse>(Server server, TResponse response) where TResponse : notnull, IErrorResponse
        {
            return ResponseAsync(server,response);
        }
        /// <summary>
        /// <see cref="OperationCanceledException"/>など特殊な例外に対して別のエラーコードを割り振りたい場合にオーバーライドしてください。
        /// </summary>
        /// <typeparam name="TResponse"></typeparam>
        /// <typeparam name="TError"></typeparam>
        /// <param name="response"></param>
        public ValueTask ResponseException<T>(Server server, T response) where T : notnull, IErrorResponse<Exception>
        {
            return ResponseError(server,response);
        }
    }

}
