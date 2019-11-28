using Utf8Json;

namespace RamType0.JsonRpc.Client
{
    interface IParamsSerializer<T>
    {
        public void Serialize(ref JsonWriter writer, IJsonFormatterResolver formatterResolver);
    }
}
