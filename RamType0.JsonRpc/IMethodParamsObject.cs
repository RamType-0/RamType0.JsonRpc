using System;
using System.Threading;
using Utf8Json;

namespace RamType0.JsonRpc
{

    public interface IMethodParams
    {

    }

    public interface IEmptyParams : IMethodParams
    {

    }


    public interface ICancellableMethodParams : IMethodParams
    {
        public CancellationToken CancellationToken { get; set; }
    }

    public struct DefaultObjectStyleParamsDeserializer<T> : IObjectStyleParamsDeserializer<T>
            where T : struct, IMethodParams
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);
        }
    }

    public interface IParamsDeserializer<T>
        where T : struct, IMethodParams
    {
        T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
    }


    public readonly struct ParamsDeserializer<T, TObjectStyle, TArrayStyle> : IParamsDeserializer<T>
        where T : struct, IMethodParams
        where TObjectStyle : struct, IObjectStyleParamsDeserializer<T>
        where TArrayStyle : struct, IArrayStyleParamsDeserializer<T>
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            T paramsObj;
            switch (reader.GetCurrentJsonToken())
            {
                case JsonToken.BeginObject:
                    {
                        paramsObj = default(TObjectStyle).Deserialize(ref reader, formatterResolver);
                        break;
                    }
                case JsonToken.BeginArray:
                    {
                        paramsObj = default(TArrayStyle).Deserialize(ref reader, formatterResolver);
                        break;
                    }
                default:
                    {
                        throw new JsonParsingException("ParamsObject was not array, neither object.");
                    }
            }
            return paramsObj;
        }



    }

    public interface IArrayStyleParamsDeserializer<T> : IParamsDeserializer<T>
        where T : struct, IMethodParams
    {


    }
    public interface IObjectStyleParamsDeserializer<T> : IParamsDeserializer<T>
        where T : struct, IMethodParams
    {

    }
}
