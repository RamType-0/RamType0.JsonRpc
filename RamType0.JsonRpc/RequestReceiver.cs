using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
using Utf8Json.Internal;

namespace RamType0.JsonRpc
{
    public class RequestReceiver
    {
        public RequestReceiver(JsonRpcMethodDictionary rpcMethodDictionary, IResponser responser, IJsonFormatterResolver jsonFormatterResolver)
        {
            RpcMethodDictionary = rpcMethodDictionary;
            Responser = responser;
            JsonFormatterResolver = jsonFormatterResolver;
        }

        JsonRpcMethodDictionary RpcMethodDictionary { get; }
        IResponser Responser { get; }
        IJsonFormatterResolver JsonFormatterResolver { get; }

        ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Shared;

        /// <summary>
        /// Taskがreturnされた時点で、引数のArraySegmentに対する操作は終了しています。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public Task Resolve(ArraySegment<byte> json)
        {
            //var reader = new JsonReader(json.Array, json.Offset);
            return RequestObjectSolver.ResolveRequest(RpcMethodDictionary, Responser, json,JsonFormatterResolver);
        }
        /// <summary>
        /// 現状の実装では配列にコピーしていますが、将来的に内部差し替えだけでSpan対応できるように。
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public Task Resolve(ReadOnlySpan<byte> json)
        {
            var buf = ArrayPool.Rent(json.Length);
            json.CopyTo(buf);
            //TODO:コピー完了時点で制御戻して良いはずだが・・・・
            var ret = Resolve(new ArraySegment<byte>(buf, 0, json.Length));
            ArrayPool.Return(buf);
            return ret;
        }

        /// <summary>
        /// 次のJsonRpcリクエストが既にStreamに乗っていた場合、ParseErrorがInvalidRequestとして報告される場合があります。(厳密に区別しようがない)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public Task Resolve(Stream input)
        {
            if(input is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buf2))
            {
                // when token is number, can not use from pool(can not find end line).
                //var token = new JsonReader(buf2.Array, buf2.Offset).GetCurrentJsonToken();
                /*if (token == JsonToken.Number)
                {

                    
                    var buf3 = new byte[buf2.Count];
                    Buffer.BlockCopy(buf2.Array!, buf2.Offset, buf3, 0, buf2.Count);

                    return Resolve(buf3);
 
                }*/

                return Resolve(buf2);
            }
            else
            {

                var buf = ArrayPool.Rent(65536);
                var len = FillFromStream(input, ref buf);

                // when token is number, can not use from pool(can not find end line).
                var token = new JsonReader(buf).GetCurrentJsonToken();
                /*if (token == JsonToken.Number)
                {
                     
                    var newBuf = BinaryUtil.FastCloneWithResize(buf, len);
                    ArrayPool.Return(buf);
                    return Resolve(newBuf);
                }
                else*/
                {
                    var ret = Resolve(buf);
                    ArrayPool.Return(buf);
                    return ret;
                }

                
            }
        }
        int FillFromStream(Stream input, ref byte[] buffer)
        {
            int length = 0;
            int read;
            while ((read = input.Read(buffer, length, buffer.Length - length)) > 0)
            {
                length += read;
                if (length == buffer.Length)
                {
                    var newBuf = ArrayPool.Rent(length * 2);
                    Buffer.BlockCopy(buffer, 0, newBuf, 0,length);
                    ArrayPool.Return(buffer);
                    buffer = newBuf;
                    //BinaryUtil.FastResize(ref buffer, length * 2);
                }
            }

            return length;
        }
        public Task Resolve(string json)
        {
            var buffer = ArrayPool.Rent(sizeof(char) *json.Length * 2);//UTF16ではコードポイントは最小2バイト、UTF8では最大4バイトなので最大でも2倍のサイズにおさまる//TODO:UTF16の2バイトコードポイントはUTF8でも3バイト以内である、みたいなことがあるかも？
            var length = Encoding.UTF8.GetBytes(json, buffer);
            var segment = new ArraySegment<byte>(buffer, 0, length);
            var ret = Resolve(segment);
            ArrayPool.Return(buffer);
            return ret;
        }

       
    }
    public struct RequestReceiverObject
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter.Nullable))]
        public JsonRpcVersion? jsonrpc;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp.Nullable))]
        public EscapedUTF8String? method;
        [IgnoreDataMember]
        public DummyParams @params;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID? id;
    }

    public struct DummyParams
    {
        /*
        class Formatter : IJsonFormatter<DummyParams>
        {
            public Formatter Instance { get; } = new Formatter();
            public DummyParams Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                reader.ReadNextBlock();
                return default;
            }

            public void Serialize(ref JsonWriter writer, DummyParams value, IJsonFormatterResolver formatterResolver)
            {
                throw new NotSupportedException();
            }
        }
        */
    }

    internal static class RequestObjectSolver// : IJsonFormatter<RequestReceiverObject>
    {
        //internal static InvokingFormatter Instance { get; } = new InvokingFormatter();//=> instance ??= new ResolvingFormatter();

        public static Task ResolveRequest(JsonRpcMethodDictionary methodDictionary, IResponser responser, ArraySegment<byte> json, IJsonFormatterResolver formatterResolver)
        {
            var reader = new JsonReader(json.Array, json.Offset);
            var copyReader = reader;
            RequestReceiverObject request;
            try
            {
                request = formatterResolver.GetFormatter<RequestReceiverObject>().Deserialize(ref reader, formatterResolver);
            }
            catch (JsonParsingException ex)
            {
                //文法エラーなの？それとも型不一致？
                try
                {

                    var jsonSegment = copyReader.ReadNextBlockSegment();
                    copyReader.SkipWhiteSpace();
                    var jsonTerminal = json.Offset + json.Count;

                    int readerOffset = copyReader.GetCurrentOffsetUnsafe();
                    if (readerOffset >= jsonTerminal)//1個分丸ごとスキップ、さらに空白もスキップした後にまだ終端に達していなかったらおかしい
                    //Jsonの文法はOK=InvalidRequest
                    {
                        return Task.Run(() => responser.ResponseError(ErrorResponse.InvalidRequest(Encoding.UTF8.GetString(jsonSegment))));//TODO:ここもクロージャプーリングする
                    }
                    else
                    {
                        return Task.Run(() => responser.ResponseException(ErrorResponse.ParseError(ex)));
                    }
                    
                }
                catch (JsonParsingException e)
                {
                    //正常にこのオブジェクトを読み飛ばせない=Jsonの文法がおかしい=ParseError
                    return Task.Run(() => responser.ResponseException(ErrorResponse.ParseError(e)));//TODO:ここもクロージャプーリングする
                }

            }

            if (request.jsonrpc is JsonRpcVersion)
            {
                if (request.method is EscapedUTF8String MethodName)
                {
                    return methodDictionary.InvokeAsync(responser, MethodName, request.id, ref copyReader, formatterResolver);
                }
                else
                {
                    return Task.Run(() => responser.ResponseError(new ErrorResponse(request.id, new ErrorObject(ErrorCode.InvalidRequest, "\"method\" property is missing."))));
                }
            }
            else
            {
                return Task.Run(() => responser.ResponseError(new ErrorResponse(request.id, new ErrorObject(ErrorCode.InvalidRequest, "\"jsonrpc\" property is missing."))));
            }



        }


    }

}
