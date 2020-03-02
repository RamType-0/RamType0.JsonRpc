using Utf8Json;

namespace RamType0.JsonRpc.Server
{
    public interface IParamsDeserializer<T>
    {
        T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver);
    }
    public interface IArrayStyleParamsDeserializer<T> : IParamsDeserializer<T>
        where T : struct
    {


    }
    public interface IObjectStyleParamsDeserializer<T> : IParamsDeserializer<T>
        where T : struct
    {

    }

    public struct DefaultObjectStyleParamsDeserializer<T> : IObjectStyleParamsDeserializer<T>
           where T : struct
    {
        public T Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
        {
            return formatterResolver.GetFormatterWithVerify<T>().Deserialize(ref reader, formatterResolver);
        }
    }
    public readonly struct ParamsDeserializer<T, TObjectStyle, TArrayStyle> : IParamsDeserializer<T>
        where T : struct
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

    public readonly struct EmptySerializedParamsDeserializer<T, TObjectStyle, TArrayStyle> : IParamsDeserializer<T>
        where T : struct
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
                case JsonToken.None:
                    {
                        paramsObj = default;
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

}
