using System.Text;
using Utf8Json;

namespace RamType0.JsonRpc
{

    /// <summary>
    /// JsonRpcにおいてnullとして表現される、戻り値のないリクエストのresultを示します。
    /// </summary>
    [JsonFormatter(typeof(Formatter))]
    public readonly struct NullResult
    {
        public sealed class Formatter : IJsonFormatter<NullResult>
        {
            public NullResult Deserialize(ref JsonReader reader, IJsonFormatterResolver formatterResolver)
            {
                if (!reader.ReadIsNull())
                {
                    throw new JsonParsingException($"Expected null,but \"{Encoding.UTF8.GetString(reader.ReadNextBlockSegment())}\"");
                }
                return default;
            }

            public void Serialize(ref JsonWriter writer, NullResult value, IJsonFormatterResolver formatterResolver)
            {
                writer.WriteNull();

            }
        }
    }

}
