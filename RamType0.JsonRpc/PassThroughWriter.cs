using RamType0.JsonRpc.Client;
using RamType0.JsonRpc.Duplex;
using RamType0.JsonRpc.Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipelines;
using Utf8Json;
using System.Buffers;
using System.Threading.Channels;
using System.Threading;

namespace RamType0.JsonRpc
{
    using Marshaling;
    public struct PassThroughWriter : IMessageWriter
    {
        public void WriteMessage(PipeWriter writer,ReadOnlySpan<byte> serializedResponse)
        {
            serializedResponse.CopyTo(writer.GetSpan(serializedResponse.Length));
            writer.Advance(serializedResponse.Length);
        }
    }
}
