using FluentAssertions;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Common;
using SpongeEngine.OobaboogaSharp.Models.Completion;
using SpongeEngine.OobaboogaSharp.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace SpongeEngine.OobaboogaSharp.Tests.Integration.Providers.OobaboogaSharpOpenAiCompatible
{
    public class Synchronous : IntegrationTestBase
    {
        private readonly OobaboogaSharpClient _clientOobaboogaSharpClient;

        public Synchronous(ITestOutputHelper output) : base(output)
        {
            _clientOobaboogaSharpClient = new OobaboogaSharpClient(new Options
            {
                BaseUrl = TestConfig.BaseApiUrl
            }, Logger);
        }

        [SkippableFact]
        [Trait("Category", "Integration")]
        public async Task Complete_WithSimplePrompt_ShouldWork()
        {
            Skip.If(!ServerAvailable, "API endpoint not available");

            // Arrange & Act
            var response = await _clientOobaboogaSharpClient.CompleteAsync(
                "Once upon a time",
                new CompletionOptions
                {
                    MaxTokens = 20,
                    Temperature = 0.7f,
                    TopP = 0.9f
                });

            // Assert
            response.Should().NotBeNullOrEmpty();
            Output.WriteLine($"Completion response: {response}");
        }

        [Fact]
        [Trait("Category", "Integration")]
        public async Task ChatComplete_WithInstructTemplate_ShouldWork()
        {
            Skip.If(!ServerAvailable, "API endpoint not available");

            // Arrange
            var messages = new List<ChatMessage>
            {
                new() { Role = "user", Content = "Write a short story" }
            };

            // Act
            var response = await _clientOobaboogaSharpClient.ChatCompleteAsync(
                messages,
                new ChatCompletionOptions
                {
                    Mode = "instruct",
                    InstructionTemplate = "Alpaca",
                    MaxTokens = 100
                });

            // Assert
            response.Should().NotBeNull();
            response.Choices.Should().NotBeNull();
            response.Choices.Should().NotBeEmpty("API should return at least one choice");
            response.Choices.First().Message.Should().NotBeNull("Choice should contain a message");
            response.Choices.First().Message.Content.Should().NotBeNullOrEmpty("Message should contain content");
        }
    }
}