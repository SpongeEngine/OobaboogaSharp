namespace SpongeEngine.OobaboogaSharp.Tests.Common
{
    public static class TestConfig
    {
        private const string DefaultHost = "http://127.0.0.1:5000";

        public static string BaseApiUrl => Environment.GetEnvironmentVariable("OOBABOOGA_BASE_URL") ?? $"{DefaultHost}";
            
        // Extended timeout for large models
        public static int TimeoutSeconds => 120;
    }
}