using System.Net.Http.Headers;
using SpongeEngine.OobaboogaSharp.Models.Common;

namespace SpongeEngine.OobaboogaSharp.Utils
{
    public static class HttpClientFactory
    {
        public static HttpClient Create(Options options)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };

            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
            }

            return client;
        }
    }
}