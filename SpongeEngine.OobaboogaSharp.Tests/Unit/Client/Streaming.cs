using FluentAssertions;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Common;
using SpongeEngine.OobaboogaSharp.Models.Completion;
using SpongeEngine.OobaboogaSharp.Tests.Common;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using Xunit.Abstractions;
using Exception = SpongeEngine.OobaboogaSharp.Models.Common.Exception;

namespace SpongeEngine.OobaboogaSharp.Tests.Unit.Client
{
    public class Streaming : UnitTestBase
    {
        private readonly OobaboogaSharpClient _clientOobaboogaSharpClient;

        public Streaming(ITestOutputHelper output) : base(output)
        {
            _clientOobaboogaSharpClient = new OobaboogaSharpClient(new Options
            {
                BaseUrl = BaseUrl
            }, Logger);
        }

        [Fact]
        public async Task StreamChatComplete_ShouldWork()
        {
            // Arrange
            var streamResponses = new[]
            {
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"role\": \"assistant\"}}]}\n\n",
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"content\": \"Hello\"}}]}\n\n",
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"content\": \" world\"}}]}\n\n",
                "data: {\"choices\": [{\"index\": 0, \"delta\": {\"content\": \"!\"}}]}\n\n",
                "data: [DONE]\n\n"
            };

            Server
                .Given(Request.Create()
                    .WithPath("/v1/chat/completions")
                    .WithBody(body => body.Contains("\"stream\":true"))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(string.Join("", streamResponses))
                    .WithHeader("Content-Type", "text/event-stream"));

            // Act
            var messages = new List<ChatMessage> 
            { 
                new() { Role = "user", Content = "Hi" } 
            };
            var receivedMessages = new List<ChatMessage>();
        
            await foreach (var message in _clientOobaboogaSharpClient.StreamChatCompletionAsync(messages))
            {
                receivedMessages.Add(message);
            }

            // Assert
            receivedMessages.Should().HaveCount(3);
            string.Concat(receivedMessages.Select(m => m.Content))
                .Should().Be("Hello world!");
        }

        [Fact]
        public async Task StreamCompletion_WithStopSequence_ShouldWork()
        {
            // Arrange
            var tokens = new[] { "Hello", " world", "!" };
            var streamResponses = tokens.Select(token => 
                $"data: {{\"choices\": [{{\"text\": \"{token}\"}}]}}\n\n");

            Server
                .Given(Request.Create()
                    .WithPath("/v1/completions")
                    .WithBody(body => body.Contains("\"stop\":[\".\""))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(string.Join("", streamResponses))
                    .WithHeader("Content-Type", "text/event-stream"));

            // Act
            var receivedTokens = new List<string>();
            await foreach (var token in _clientOobaboogaSharpClient.StreamCompletionAsync(
                "Test prompt",
                new CompletionOptions { StopSequences = new[] { "." } }))
            {
                receivedTokens.Add(token);
            }

            // Assert
            receivedTokens.Should().BeEquivalentTo(tokens);
        }

        [Fact]
        public async Task StreamChatComplete_WithError_ShouldFail()
        {
            // Arrange
            Server
                .Given(Request.Create()
                    .WithPath("/v1/chat/completions")
                    .WithBody(body => body.Contains("\"stream\":true"))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(500)
                    .WithBody("Internal error"));

            // Act & Assert
            var messages = new List<ChatMessage> 
            { 
                new() { Role = "user", Content = "Hi" } 
            };

            // Using Func<Task> for async assertions
            Func<Task> act = async () => 
            {
                await foreach (var _ in _clientOobaboogaSharpClient.StreamChatCompletionAsync(messages))
                {
                    // Should throw before yielding any results
                }
            };

            // Use Should().ThrowAsync<T>() for async operations
            await act.Should().ThrowAsync<Exception>();
        }
    }
}