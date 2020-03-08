using System;

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
