using System.Runtime.Serialization;
using Utf8Json;

namespace RamType0.JsonRpc
{

    public struct Request<T>
    //where T : notnull, IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp))]
        [DataMember(Name = "method")]
        public EscapedUTF8String Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
        [DataMember(Name = "id")]
        public ID ID { get; set; }
    }

    public struct Notification<T>
    //where T : notnull, IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp))]
        [DataMember(Name = "method")]
        public EscapedUTF8String Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
    }



}