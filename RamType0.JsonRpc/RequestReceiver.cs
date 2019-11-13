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
            JsonSerializer.Deserialize<DummyRequestObject>(json.Array, json.Offset);
        }
        struct DummyRequestObject
        {

            
        }
        /// <summary>
        /// 手法が邪悪すぎる
        /// </summary>
        internal sealed class ResolvingFormatter : IJsonFormatter<DummyRequestObject>
        {
            //[field: ThreadStatic]
            //static ResolvingFormatter? instance;
            /// <summary>
            /// 状態を持たないので・・・
            /// </summary>
            internal static ResolvingFormatter Instance { get; } = new ResolvingFormatter();//=> instance ??= new ResolvingFormatter();
            public DummyRequestObject Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                if (!reader.ReadIsValidJsonRpcMember())
                {
                    //TODO:不正フォーマットレスポンス送信フラグを立てる
                }
                reader.ReadPropertyNameSegmentRaw().AsSpan().SequenceEqual(stackalloc byte[] { (byte)'m', (byte)'e', (byte)'t', (byte)'h', (byte)'o', (byte)'d', });
                var methodNameUnEscaped = reader.ReadStringSegmentRaw();
                
                }

            public void Serialize(ref JsonWriter writer, DummyRequestObject value, IJsonFormatterResolver formatterResolver)
            {
                throw new NotImplementedException();
            }
        }
        sealed class FormatterResolver : IJsonFormatterResolver
        {
            internal static FormatterResolver Instance { get; } = new FormatterResolver();
            public IJsonFormatter<T> GetFormatter<T>()
            {
                if(typeof(T) == typeof(DummyRequestObject))
                {
                    return Unsafe.As<IJsonFormatter<T>> (ResolvingFormatter.Instance);
                }
                if(typeof(T) == typeof(ID))
                {
                    return Unsafe.As<IJsonFormatter<T>>(ID.Formatter.Instance);
                }
                return JsonSerializer.DefaultResolver.GetFormatter<T>();
            }
        }
    }
}
