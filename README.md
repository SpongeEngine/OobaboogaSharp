# OobaboogaSharp
[![NuGet](https://img.shields.io/nuget/v/SpongeEngine.OobaboogaSharp.svg)](https://www.nuget.org/packages/SpongeEngine.OobaboogaSharp)
[![NuGet Downloads](https://img.shields.io/nuget/dt/SpongeEngine.OobaboogaSharp.svg)](https://www.nuget.org/packages/SpongeEngine.OobaboogaSharp)
[![License](https://img.shields.io/github/license/SpongeEngine/OobaboogaSharp)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%2B-512BD4)](https://dotnet.microsoft.com/download)

C# client for interacting with Oobabooga's text-generation-webui through its OpenAI-compatible API endpoints.

## Features
- OpenAI-compatible API support
- Text completion and chat completion 
- Streaming responses support
- Character templates and instruction formats
- Built-in error handling and logging
- Cross-platform compatibility
- Full async/await support

## Installation
```bash
dotnet add package SpongeEngine.OobaboogaSharp
```

## Quick Start

```csharp
using SpongeEngine.OobaboogaSharp;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Completion;

// Create client instance
var client = new OobaboogaSharpClient(new OobaboogaSharpClientOptions
{
    HttpClient = new HttpClient 
    {
        BaseAddress = new Uri("http://localhost:5000")
    }
});

// Simple completion
var response = await client.CompleteAsync(
    "Write a short story about a robot:",
    new CompletionOptions
    {
        MaxTokens = 200,
        Temperature = 0.7f,
        TopP = 0.9f
    });

Console.WriteLine(response);

// Chat completion
var messages = new List<ChatMessage>
{
    new() { Role = "user", Content = "Write a poem about coding" }
};

var chatResponse = await client.ChatCompleteAsync(
    messages,
    new ChatCompletionOptions
    {
        Mode = "instruct",
        InstructionTemplate = "Alpaca",
        MaxTokens = 200
    });

Console.WriteLine(chatResponse.Choices[0].Message.Content);

// Stream chat completion
await foreach (var message in client.StreamChatCompletionAsync(messages))
{
    Console.Write(message.Content);
}
```

## Configuration Options

### Basic Options
```csharp
var options = new Options
{
    BaseUrl = "http://localhost:5000",    // text-generation-webui server URL
    ApiKey = "optional_api_key",          // Optional API key for authentication
    TimeoutSeconds = 120                  // Request timeout
};
```

### Chat Completion Options
```csharp
var options = new ChatCompletionOptions
{
    ModelName = "model_name",           // Optional
    MaxTokens = 200,
    Temperature = 0.7f,
    TopP = 0.9f,
    StopSequences = new[] { "\n" },
    Mode = "chat",                      // "chat" or "instruct"
    InstructionTemplate = "Alpaca",     // For instruct mode
    Character = "Assistant"             // Optional character template
};
```

### Text Completion Options
```csharp
var options = new CompletionOptions
{
    ModelName = "model_name",          // Optional
    MaxTokens = 200,
    Temperature = 0.7f,
    TopP = 0.9f,
    StopSequences = new[] { "\n" }
};
```

## Error Handling
```csharp
try
{
    var response = await client.ChatCompleteAsync(messages, options);
}
catch (Exception ex) when (ex is SpongeEngine.OobaboogaSharp.Models.Common.Exception oobaboogaEx)
{
    Console.WriteLine($"Error: {ex.Message}");
    Console.WriteLine($"Status code: {oobaboogaEx.StatusCode}");
    Console.WriteLine($"Response: {oobaboogaEx.ResponseContent}");
}
```

## Logging
```csharp
var client = new OobaboogaSharpClient(new OobaboogaSharpClientOptions
{
    HttpClient = new HttpClient 
    {
        BaseAddress = new Uri("http://localhost:5000")
    },
    Logger = LoggerFactory
        .Create(builder => builder.AddConsole())
        .CreateLogger<OobaboogaSharpClient>()
});
```

## Testing
The library includes both unit and integration tests. Integration tests require a running text-generation-webui server.

To run the tests:
```bash
dotnet test
```

Configure test environment:
```csharp
Environment.SetEnvironmentVariable("OOBABOOGA_BASE_URL", "http://localhost:5000");
```

## License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

## Support
For issues and feature requests, please use the [GitHub issues page](https://github.com/SpongeEngine/OobaboogaSharp/issues).
