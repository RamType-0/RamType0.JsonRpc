using System.Runtime.Serialization;
using Utf8Json;

namespace RamType0.JsonRpc.Client
{
    public struct Request<T>
        where T:notnull,IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp.Nullable))]
        [DataMember(Name = "method")]
        public EscapedUTF8String Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
        [DataMember(Name = "id")]
        public ID ID { get; set; }
    }

    public struct Notification<T>
        where T : notnull, IMethodParams
    {
        [JsonFormatter(typeof(JsonRpcVersion.Formatter))]
        [DataMember(Name = "jsonrpc")]
        public JsonRpcVersion Version => default;
        [JsonFormatter(typeof(EscapedUTF8String.Formatter.Temp.Nullable))]
        [DataMember(Name = "method")]
        public EscapedUTF8String Method { get; set; }
        [DataMember(Name = "params")]
        public T Params { get; set; }
    }


    /// <summary>
    /// レスポンスのデシリアライズ用の構造体。レスポンスのフォーマットが不正のときに関する仕様がないため、特にハンドリングしていない・・・
    /// </summary>
    public struct ResponseMessage
    {
        //[DataMember(Name = "jsonrpc")]
        //public JsonRpcVersion Version { get; set; }
        //[DataMember(Name = "result")]
        //public object? Result { get; set; }
        [DataMember(Name = "id")]
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID? ID { get; set; }
        [DataMember(Name = "error")]
        public ResponseError<object?>? Error { get; set; }
    }

    public struct Response<T>
    {
        [DataMember(Name = "result")]
        public T Result { get; set; }
        [DataMember(Name = "id")]
        [JsonFormatter(typeof(ID.Formatter.Nullable))]
        public ID ID { get; set; }
        [DataMember(Name = "error")]
        public ResponseError<object?>? Error { get; set; }
    }

    public struct ResponseError<T>
    {
        [DataMember(Name = "code")]
        public long Code { get; set; }
        [DataMember(Name = "message")]
        public string Message { get; set; }
        [DataMember(Name = "data")]
        public T Data { get; set; }

    }

}
