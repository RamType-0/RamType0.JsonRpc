using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks.Sources;
using System.Threading.Channels;
using System.Threading.Tasks;
using Utf8Json;
namespace RamType0.JsonRpc.Server
{
    public class RequestReceiver
    {
        public RequestReceiver(JsonRpcMethodDictionary rpcMethodDictionary, IResponseOutput output, IJsonFormatterResolver jsonFormatterResolver)
        {
            RpcMethodDictionary = rpcMethodDictionary;
            Output = output;
            JsonFormatterResolver = jsonFormatterResolver;
        }

        JsonRpcMethodDictionary RpcMethodDictionary { get; }
        IResponseOutput Output { get; }
        IJsonFormatterResolver JsonFormatterResolver { get; }
        /// <summary>
        /// ArrayPool
        /// </summary>
        ArrayPool<byte> ArrayPool { get; } = ArrayPool<byte>.Shared;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="segment"></param>
        /// <returns>リクエストの返答までを含む処理の完了を示す<see cref="ValueTask"/>。</returns>
        ValueTask ResolveAsync(ArraySegment<byte> segment)
        {
            return RequestObjectResolver.ResolveAsync(RpcMethodDictionary, Output, segment, JsonFormatterResolver);
        }

        public void Resolve(ArraySegment<byte> segment)
        {
            RequestObjectResolver.Resolve(RpcMethodDictionary, Output, segment, JsonFormatterResolver);
        }
        /// <summary>
        /// 現状の実装では配列にコピーしていますが、将来的に内部差し替えだけでSpan対応できるように。
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public ValueTask ResolveAsync(ReadOnlySpan<byte> span)
        {
            var buf = ArrayPool.Rent(span.Length);
            span.CopyTo(buf);
            ArraySegment<byte> json = new ArraySegment<byte>(buf, 0, span.Length);
            var closure = GetClosure(this, json);
            return new ValueTask(Task.Run(closure.InvokeAction));

        }

        

        CopiedResolveClosure GetClosure(RequestReceiver receiver, ArraySegment<byte> json)
        {
            CopiedResolveClosure closure = Pool.Get();
            closure.Inject(receiver, json);
            return closure;
        }

        DefaultObjectPool<CopiedResolveClosure> Pool { get; } = new DefaultObjectPool<CopiedResolveClosure>(new CopiedResolveClosure.PooledPolicy());
        class CopiedResolveClosure
        {
            
            RequestReceiver Receiver { get; set; } = default!;
            ArraySegment<byte> Json { get; set; }
            internal void Inject(RequestReceiver receiver,ArraySegment<byte> json)
            {
                Receiver = receiver;
                Json = json;
            }

            void Invoke()
            {
                try
                {
                    Receiver.Resolve(Json);
                    Receiver.ArrayPool.Return(Json.Array!);
                }
                finally
                {
                    Receiver.Pool.Return(this);
                }
            }
            public Action InvokeAction { get; }
            internal CopiedResolveClosure()
            {
                InvokeAction = Invoke;
            }

            internal sealed class PooledPolicy : PooledObjectPolicy<CopiedResolveClosure>
            {
                public override CopiedResolveClosure Create()
                {
                    return new CopiedResolveClosure();
                }

                public override bool Return(CopiedResolveClosure obj)
                {
                    obj.Receiver = default!;
                    obj.Json = default;
                    return true;
                }
            }
        }

        /// <summary>
        /// 次のJsonRpcリクエストが既にStreamに乗っていた場合、ParseErrorがInvalidRequestとして報告される場合があります。(厳密に区別しようがない)
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public ValueTask ResolveAsync(Stream stream)
        {
            if(stream is MemoryStream memoryStream && memoryStream.TryGetBuffer(out var buf2))
            {
                // when token is number, can not use from pool(can not find end line).
                //var token = new JsonReader(buf2.Array, buf2.Offset).GetCurrentJsonToken();
                /*if (token == JsonToken.Number)
                {

                    
                    var buf3 = new byte[buf2.Count];
                    Buffer.BlockCopy(buf2.Array!, buf2.Offset, buf3, 0, buf2.Count);

                    return Resolve(buf3);
 
                }*/

                return ResolveAsync(buf2);
            }
            else
            {

                var buf = ArrayPool.Rent(65536);
                _ = FillFromStream(stream, ref buf);

                // when token is number, can not use from pool(can not find end line).
                //var token = new JsonReader(buf).GetCurrentJsonToken();
                /*if (token == JsonToken.Number)
                {
                     
                    var newBuf = BinaryUtil.FastCloneWithResize(buf, len);
                    ArrayPool.Return(buf);
                    return Resolve(newBuf);
                }
                else*/
                {
                    var ret = ResolveAsync(buf);
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
        public ValueTask Resolve(string json)
        {
            var buffer = ArrayPool.Rent(sizeof(char) *json.Length * 2);//UTF16ではコードポイントは最小2バイト、UTF8では最大4バイトなので最大でも2倍のサイズにおさまる//TODO:UTF16の2バイトコードポイントはUTF8でも3バイト以内である、みたいなことがあるかも？
            var length = Encoding.UTF8.GetBytes(json, buffer);
            var segment = new ArraySegment<byte>(buffer, 0, length);
            var ret = ResolveAsync(segment);
            ArrayPool.Return(buffer);
            return ret;
        }

       
    }
    
    public struct RequestMessage
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter.Nullable))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion? Version { get; set; }
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp.Nullable))]
        [DataMember(Name = "method")]
        public EscapedUTF8String? Method { get; set; }
        [IgnoreDataMember]
        public DummyParams Params => default;
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        [DataMember(Name = "id")]
        public ID? ID { get; set; }
    }
    public struct DummyParams
    {

    }
    public static class RequestObjectResolver// : IJsonFormatter<RequestReceiverObject>
    {
        public static ValueTask ResolveAsync(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, ArraySegment<byte> json, IJsonFormatterResolver formatterResolver)
        {
            var reader = new JsonReader(json.Array, json.Offset);
            var copyReader = reader;
            RequestMessage request;
            try
            {
                request = formatterResolver.GetFormatter<RequestMessage>().Deserialize(ref reader, formatterResolver);
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
                        return output.ResponseError(ErrorResponse.InvalidRequest(Encoding.UTF8.GetString(jsonSegment)));//TODO:ここもクロージャプーリングする
                    }
                    else
                    {
                        return output.ResponseException(ErrorResponse.ParseError(ex));
                    }
                    
                }
                catch (JsonParsingException e)
                {
                    //正常にこのオブジェクトを読み飛ばせない=Jsonの文法がおかしい=ParseError
                    return output.ResponseException(ErrorResponse.ParseError(e));//TODO:ここもクロージャプーリングする
                }

            }

            if (request.Version.HasValue)// is JsonRpcVersionだとボックス化が入る罠
            {
                if (request.Method is EscapedUTF8String MethodName)
                {
                    return methodDictionary.InvokeAsync(output, MethodName, request.ID, ref copyReader, formatterResolver);
                }
                else
                {
                    return output.ResponseError(new ErrorResponse(request.ID, new ErrorObject(ErrorCode.InvalidRequest, "\"method\" property is missing.")));
                }
            }
            else
            {
                return output.ResponseError(new ErrorResponse(request.ID, new ErrorObject(ErrorCode.InvalidRequest, "\"jsonrpc\" property is missing.")));
            }



        }
        public static void Resolve(JsonRpcMethodDictionary methodDictionary, IResponseOutput output, ArraySegment<byte> json, IJsonFormatterResolver formatterResolver)
        {
            var reader = new JsonReader(json.Array, json.Offset);
            var copyReader = reader;
            RequestMessage request;
            try
            {
                request = formatterResolver.GetFormatter<RequestMessage>().Deserialize(ref reader, formatterResolver);
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
                        output.ResponseError(ErrorResponse.InvalidRequest(Encoding.UTF8.GetString(jsonSegment)));//TODO:ここもクロージャプーリングする
                        return;
                    }
                    else
                    {
                        output.ResponseException(ErrorResponse.ParseError(ex));
                        return;
                    }

                }
                catch (JsonParsingException e)
                {
                    //正常にこのオブジェクトを読み飛ばせない=Jsonの文法がおかしい=ParseError
                    output.ResponseException(ErrorResponse.ParseError(e));
                    return;
                }

            }
            
            if (request.Version.HasValue)// is JsonRpcVersionだとボックス化が入る罠
            {
                if (request.Method is EscapedUTF8String MethodName)
                {
                    methodDictionary.Invoke(output, MethodName, request.ID, ref copyReader, formatterResolver);
                    return;
                }
                else
                {
                    output.ResponseError(new ErrorResponse(request.ID, new ErrorObject(ErrorCode.InvalidRequest, "\"method\" property is missing.")));
                    return;
                }
            }
            else
            {
                output.ResponseError(new ErrorResponse(request.ID, new ErrorObject(ErrorCode.InvalidRequest, "\"jsonrpc\" property is missing.")));
                return;
            }



        }
    }

}
