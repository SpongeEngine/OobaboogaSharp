using System.Text.Json.Serialization;

namespace SpongeEngine.OobaboogaSharp.Models.Chat
{
    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user";

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }
}