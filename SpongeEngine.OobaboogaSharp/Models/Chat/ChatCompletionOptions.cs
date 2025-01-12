namespace SpongeEngine.OobaboogaSharp.Models.Chat
{
    public class ChatCompletionOptions
    {
        public string? ModelName { get; set; }
        public int? MaxTokens { get; set; } = 100;
        public float? Temperature { get; set; } = 0.7f;
        public float? TopP { get; set; } = 0.9f;
        public string[]? StopSequences { get; set; }
        public string Mode { get; set; } = "chat";
        public string? InstructionTemplate { get; set; }
        public string? Character { get; set; }
    }
}