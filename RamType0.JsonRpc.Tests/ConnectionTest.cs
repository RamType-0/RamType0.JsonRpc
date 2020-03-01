using NUnit.Framework;
using RamType0.JsonRpc.Duplex;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipelines;
namespace RamType0.JsonRpc.Tests
{
    public class ConnectionTest
    {
        Pipe a2b, b2a;
        Connection a,b;
        [SetUp]
        public void Setup()
        {
            a2b = new Pipe();
            b2a = new Pipe();
            //a = new Connection(new Server.Server(new PipeMessageOutput<PassThroughWriter>(new PassThroughWriter(), a2b.Writer)),new Client.Client());
        }
        public async ValueTask AwaitingRequest()
        {

        }
    }
}
