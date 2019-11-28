using System;
using System.Collections.Generic;
using System.Text;
using RamType0.JsonRpc.Client;
using RamType0.JsonRpc.Server;
namespace RamType0.JsonRpc.Duplex
{
    public interface IDuplexOutput : IResponseOutput,IRequestOutput
    {
    }
}
