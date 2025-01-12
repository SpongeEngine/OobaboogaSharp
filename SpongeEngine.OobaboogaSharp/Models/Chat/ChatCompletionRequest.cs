using Newtonsoft.Json;

namespace SpongeEngine.OobaboogaSharp.Models.Chat 
{
    public class ChatCompletionRequest
    {
        [JsonProperty("model")]
        public string Model { get; set; } = string.Empty;

        [JsonProperty("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        [JsonProperty("temperature")]
        public float? Temperature { get; set; }

        [JsonProperty("top_p")]
        public float? TopP { get; set; }

        [JsonProperty("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonProperty("stop")]
        public string[]? StopSequences { get; set; }

        [JsonProperty("stream")]
        public bool Stream { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; } = "chat";  // "chat" or "instruct"

        [JsonProperty("instruction_template")]
        public string? InstructionTemplate { get; set; }

        [JsonProperty("character")]
        public string? Character { get; set; }
    }
}