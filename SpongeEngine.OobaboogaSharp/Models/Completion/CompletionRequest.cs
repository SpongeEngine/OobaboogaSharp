using Newtonsoft.Json;

namespace SpongeEngine.OobaboogaSharp.Models.Completion
{
    public class CompletionRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonProperty("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonProperty("temperature")]
        public float? Temperature { get; set; }

        [JsonProperty("top_p")]
        public float? TopP { get; set; }

        [JsonProperty("stop")]
        public string[]? StopSequences { get; set; }

        [JsonProperty("stream")]
        public bool Stream { get; set; }

        [JsonProperty("seed")]
        public int? Seed { get; set; }
    }
}