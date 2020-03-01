using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Text;

namespace RamType0.JsonRpc.Marshaling
{
    public readonly struct HeaderDelimitedMessageWriter : IMessageWriter
    {
        public void WriteResponse(PipeWriter writer, ReadOnlySpan<byte> serializedResponse)
        {
            var span = writer.GetSpan(Header.MaxHeaderSize + serializedResponse.Length);
            var header = new Header(serializedResponse.Length);
            var headerSize = header.Write(span);
            serializedResponse.CopyTo(span[headerSize..]);
            writer.Advance(headerSize + serializedResponse.Length);
        }
    }
}
