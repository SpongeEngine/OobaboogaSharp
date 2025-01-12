using Xunit;
using Xunit.Abstractions;

namespace SpongeEngine.OobaboogaSharp.Tests.Common
{
    public abstract class IntegrationTestBase : TestBase, IAsyncLifetime
    {
        protected bool ServerAvailable;

        protected IntegrationTestBase(ITestOutputHelper output) : base(output)
        {
        }

        public virtual async Task InitializeAsync()
        {
            try
            {
                using var httpClient = new HttpClient();
                var response = await httpClient.GetAsync($"{TestConfig.BaseApiUrl}/v1/models");
                ServerAvailable = response.IsSuccessStatusCode;

                if (ServerAvailable)
                {
                    Output.WriteLine("API endpoint is available");
                }
                else
                {
                    Output.WriteLine("API endpoint is not available");
                    throw new SkipTestException("API endpoint is not available");
                }
            }
            catch (Exception ex) when (ex is not SkipTestException)
            {
                Output.WriteLine($"Failed to connect to API endpoint: {ex.Message}");
                throw new SkipTestException("Failed to connect to API endpoint");
            }
        }

        public virtual Task DisposeAsync() => Task.CompletedTask;

        protected class SkipTestException : Exception
        {
            public SkipTestException(string message) : base(message) { }
        }
    }
}