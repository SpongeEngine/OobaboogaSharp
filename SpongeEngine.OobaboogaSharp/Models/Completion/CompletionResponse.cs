using System.Text.Json.Serialization;

namespace SpongeEngine.OobaboogaSharp.Models.Completion
{
    public class CompletionResponse
    {
        [JsonPropertyName("choices")]
        public List<Choice> Choices { get; set; } = new();

        public class Choice
        {
            [JsonPropertyName("text")]
            public string Text { get; set; } = string.Empty;
        }
    }
}