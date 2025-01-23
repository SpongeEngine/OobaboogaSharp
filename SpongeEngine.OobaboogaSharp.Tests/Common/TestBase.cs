using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SpongeEngine.OobaboogaSharp.Tests.Common
{
    public abstract class TestBase
    {
        protected readonly ITestOutputHelper Output;
        
        protected TestBase(ITestOutputHelper output)
        {
            Output = output;
        }
        
        private const string DefaultHost = "http://127.0.0.1:5000";

        public static string BaseApiUrl => Environment.GetEnvironmentVariable("OOBABOOGA_BASE_URL") ?? $"{DefaultHost}";
            
        // Extended timeout for large models
        public static int TimeoutSeconds => 120;
    }
}