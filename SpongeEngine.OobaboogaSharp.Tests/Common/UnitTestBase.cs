using WireMock.Server;
using Xunit.Abstractions;

namespace SpongeEngine.OobaboogaSharp.Tests.Common
{
    public abstract class UnitTestBase : TestBase
    {
        protected readonly WireMockServer Server;

        protected UnitTestBase(ITestOutputHelper output) : base(output)
        {
            Server = WireMockServer.Start();
        }
    }
}