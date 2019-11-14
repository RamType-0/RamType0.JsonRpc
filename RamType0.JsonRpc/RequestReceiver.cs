using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Utf8Json;
namespace RamType0.JsonRpc
{
    class RequestReceiver
    {
        public Task Resolve(ArraySegment<byte> json)
        {
            JsonSerializer.Deserialize<RequestReceiverObject>(json.Array, json.Offset);
        }
        public struct RequestReceiverObject
        {
            //jsonrpc
            public EscapedUTF8String Method { get; }
            //params?
            public ID? ID { get; }
        }
        /// <summary>
        /// 手法が邪悪すぎる
        /// </summary>
        internal sealed class ResolvingFormatter : IJsonFormatter<RequestReceiverObject>
        {
            //[field: ThreadStatic]
            //static ResolvingFormatter? instance;
            /// <summary>
            /// 状態を持たないので・・・
            /// </summary>
            internal static ResolvingFormatter Instance { get; } = new ResolvingFormatter();//=> instance ??= new ResolvingFormatter();
            /// <summary>
            /// 内部バッファを直接参照しているため、ここでreturnされるmethodNameは放っておくと勝手に書き換わります。即座に使用してください。
            /// </summary>
            /// <param name="reader"></param>
            /// <param name="formatterResolver"></param>
            /// <returns></returns>
            public RequestReceiverObject Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                if (!reader.ReadIsValidJsonRpcMember())
                {
                    //TODO:不正フォーマットレスポンス送信フラグを立てる
                }
                    reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d', });
                    var methodName = EscapedUTF8String.Formatter.Instance.DeserializeUnsafe(ref reader);
                    
                }

            public void Serialize(ref JsonWriter writer, RequestReceiverObject value, IJsonFormatterResolver formatterResolver)
            {
                throw new NotImplementedException();
            }
        }
        sealed class FormatterResolver : IJsonFormatterResolver
        {
            internal static FormatterResolver Instance { get; } = new FormatterResolver();
            public IJsonFormatter<T> GetFormatter<T>()
            {
                if(typeof(T) == typeof(RequestReceiverObject))
                {
                    return Unsafe.As<IJsonFormatter<T>> (ResolvingFormatter.Instance);
                }
                if(typeof(T) == typeof(ID))
                {
                    return Unsafe.As<IJsonFormatter<T>>(ID.Formatter.Instance);
                }
                if (typeof(T) == typeof(JsonRpcVersion))
                {
                    return Unsafe.As<IJsonFormatter<T>>(JsonRpcVersion.Formatter.Instance);
                }

                return JsonSerializer.DefaultResolver.GetFormatter<T>();
            }
        }
    }
}
