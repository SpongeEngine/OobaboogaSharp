namespace SpongeEngine.OobaboogaSharp.Models.Common
{
    public class Options
    {
        public string BaseUrl { get; set; } = "http://localhost:5000";
        public string? ApiKey { get; set; }
        public int TimeoutSeconds { get; set; } = 120;
    }
}