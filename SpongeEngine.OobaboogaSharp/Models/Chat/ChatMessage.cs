using Newtonsoft.Json;

namespace SpongeEngine.OobaboogaSharp.Models.Chat
{
    public class ChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = "user";

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }
}