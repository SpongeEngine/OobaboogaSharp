using System.Text.Json.Serialization;

namespace SpongeEngine.OobaboogaSharp.Models.Chat 
{
    public class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("temperature")]
        public float? Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public float? TopP { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("stop")]
        public string[]? StopSequences { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "chat";  // "chat" or "instruct"

        [JsonPropertyName("instruction_template")]
        public string? InstructionTemplate { get; set; }

        [JsonPropertyName("character")]
        public string? Character { get; set; }
    }
}