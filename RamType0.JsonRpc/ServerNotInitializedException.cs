using System;
using System.Collections.Generic;
using System.Text;

namespace RamType0.JsonRpc
{
    [Serializable]
    public class ServerNotInitializedException : InvalidOperationException
    {
        public ServerNotInitializedException() { }
        public ServerNotInitializedException(string message) : base(message) { }
        public ServerNotInitializedException(string message, Exception inner) : base(message, inner) { }

    }
}
