﻿using Newtonsoft.Json;

namespace LocalAI.NET.Oobabooga.Models.Chat
{
    public class OobaboogaChatMessage
    {
        [JsonProperty("role")]
        public string Role { get; set; } = "user";

        [JsonProperty("content")]
        public string Content { get; set; } = string.Empty;
    }
}