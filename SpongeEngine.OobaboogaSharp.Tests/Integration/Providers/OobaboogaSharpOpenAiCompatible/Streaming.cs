using FluentAssertions;
using Microsoft.Extensions.Logging;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Common;
using SpongeEngine.OobaboogaSharp.Models.Completion;
using SpongeEngine.OobaboogaSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace SpongeEngine.OobaboogaSharp.Tests.Integration.Providers.OobaboogaSharpOpenAiCompatible
{
    public class Streaming : IntegrationTestBase
    {
        
        private readonly OobaboogaSharpClient _clientOobaboogaSharpClient;
        private readonly ILogger<Streaming> _logger;


        public Streaming(ITestOutputHelper output) : base(output)
        {
            _logger = LoggerFactory
                .Create(builder => builder.AddXUnit(output))
                .CreateLogger<Streaming>();

            _clientOobaboogaSharpClient = new OobaboogaSharpClient(new Options
            {
                BaseUrl = TestConfig.BaseApiUrl
            }, _logger);
        }

        [SkippableFact]
        [Trait("Category", "Integration")]
        public async Task StreamChatCompletion_ShouldWork()
        {
            Skip.If(!ServerAvailable, "API endpoint not available");

            // Arrange
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Tell me a very short story about a cat." }
            };
            var options = new ChatCompletionOptions
            {
                Mode = "instruct",
                InstructionTemplate = "Alpaca",
                MaxTokens = 100,
                Temperature = 0.7f,
                TopP = 0.9f
            };

            // Act
            var chatMessages = new List<ChatMessage>();
            await foreach (var message in _clientOobaboogaSharpClient.StreamChatCompletionAsync(messages, options))
            {
                _logger.LogInformation("Received message: {Content}", message.Content);
                chatMessages.Add(message);
            }

            // Assert
            chatMessages.Should().NotBeEmpty("we should receive at least one message");
            chatMessages.Should().OnlyContain(m => !string.IsNullOrWhiteSpace(m.Content), 
                "all messages should have content");
        }

        [SkippableFact]
        [Trait("Category", "Integration")]
        public async Task StreamCompletion_WithTimeout_ShouldComplete()
        {
            Skip.If(!ServerAvailable, "API endpoint not available");

            // Arrange
            var options = new CompletionOptions
            {
                MaxTokens = 20,
                Temperature = 0.7f,
                TopP = 0.9f
            };

            // Act
            var tokens = new List<string>();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            try
            {
                await foreach (var token in _clientOobaboogaSharpClient.StreamCompletionAsync(
                    "Write a short story about",
                    options,
                    cts.Token))
                {
                    tokens.Add(token);
                    Output.WriteLine($"Received token: {token}");

                    if (tokens.Count >= options.MaxTokens)
                        break;
                }
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                Output.WriteLine($"Stream timed out after receiving {tokens.Count} tokens");
            }

            // Assert
            tokens.Should().NotBeEmpty();
            string.Concat(tokens).Should().NotBeNullOrEmpty();
        }
    }
}