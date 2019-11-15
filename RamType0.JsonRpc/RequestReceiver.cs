using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
namespace RamType0.JsonRpc
{
    public class RequestReceiver
    {
        public Task Resolve(ArraySegment<byte> json)
        {
            JsonSerializer.Deserialize<RequestReceiverObject>(json.Array, json.Offset);
            throw new NotImplementedException();
        }
        public struct RequestReceiverObject
        {
            public JsonRpcVersion jsonrpc;
            public EscapedUTF8String method;
            [IgnoreDataMember]
            public DummyParams @params;
            
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

        

        /// <summary>
        /// 手法が邪悪すぎる
        /// </summary>
        internal sealed class InvokingFormatter : IJsonFormatter<RequestReceiverObject>
        {
            JsonRpcMethodDictionary JsonRpcMethodDictionary { get; }
            internal static InvokingFormatter Instance { get; } = new InvokingFormatter();//=> instance ??= new ResolvingFormatter();
            /// <summary>
            /// 内部バッファを直接参照しているため、ここでreturnされるmethodNameは放っておくと勝手に書き換わります。即座に使用してください。
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="formatterResolver"></param>
            /// <returns></returns>
            public RequestReceiverObject Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                var copyReader = reader;
                var requestReader = reader;
                RequestReceiverObject request;
                try
                {
                    request = formatterResolver.GetFormatter<RequestReceiverObject>().Deserialize(ref requestReader, formatterResolver);
                }
                catch(JsonParsingException)
                {
                    //文法エラーなの？それとも型不一致？
                    try
                    {
                        copyReader.ReadNextBlock();
                        //正常にこのオブジェクトを読み飛ばせる=Jsonの文法はOK=InvalidRequest
                        throw;
                    }
                    catch (JsonParsingException)
                    {
                        //正常にこのオブジェクトを読み飛ばせない=Jsonの文法がおかしい=ParseError
                        throw;
                    }
                    
                }
                
                copyReader.ReadIsBeginObjectWithVerify();
                ReadOnlySpan<byte> paramsStr = stackalloc byte[] { (byte)'p', (byte)'a', (byte)'r', (byte)'a', (byte)'m', (byte)'s', };
                try
                {
                    while (!copyReader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(paramsStr))
                    {
                        copyReader.ReadNextBlock();
                    }
                }
                catch (JsonParsingException)
                {
                    //paramsが見つからない=InvalidRequest
                }
                //DeserializeParams
                throw new NotImplementedException();


            }

            private static bool ReadIsMethodPropertyName(ref JsonReader reader)
            {
                return reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d', });
            }

            public void Serialize(ref JsonWriter writer, RequestReceiverObject value, IJsonFormatterResolver formatterResolver)
            {
                throw new NotImplementedException();
            }
        }
       
    }
}
