using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SpongeEngine.OobaboogaSharp.Tests.Common
{
    public abstract class TestBase : IDisposable
    {
        protected readonly ILogger Logger;
        protected readonly ITestOutputHelper Output;

        protected TestBase(ITestOutputHelper output)
        {
            Output = output;
            Logger = LoggerFactory
                .Create(builder => builder.AddXUnit(output))
                .CreateLogger(GetType());
        }

        public virtual void Dispose()
        {
            // Cleanup if needed
        }
    }
}