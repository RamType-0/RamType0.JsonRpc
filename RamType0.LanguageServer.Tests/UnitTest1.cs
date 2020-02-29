using NUnit.Framework;
using System.Threading.Tasks;

namespace RamType0.LanguageServer.Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async ValueTask Test1()
        {
            var server = new LanguageServer();
            var connection = server.Connect();
            await Task.Delay(10000);
            var disposing = server.DisposeAsync();
            await connection;
            await disposing;
        }
    }
}