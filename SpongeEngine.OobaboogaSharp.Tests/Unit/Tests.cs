using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Tests.Common;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using Xunit.Abstractions;

namespace SpongeEngine.OobaboogaSharp.Tests.Unit
{
    public class Tests : UnitTestBase
    {
        private OobaboogaSharpClient Client { get; }

        public Tests(ITestOutputHelper output) : base(output)
        {
            Client = new OobaboogaSharpClient(new OobaboogaSharpClientOptions()
            {
                HttpClient = new HttpClient 
                {
                    BaseAddress = new Uri(Server.Urls[0])
                },
                 Logger = LoggerFactory
                    .Create(builder => builder.AddXUnit(output))
                    .CreateLogger<Tests>(),
            });
        }

        [Fact]
        public async Task CompleteAsync_ShouldReturnValidResponse()
        {
            // Arrange
            const string expectedResponse = "Test response";
            Server
                .Given(Request.Create()
                    .WithPath("/v1/completions")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody($"{{\"choices\": [{{\"text\": \"{expectedResponse}\"}}]}}"));

            // Act
            var response = await Client.CompleteAsync("Test prompt");

            // Assert
            response.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task ChatCompleteAsync_ShouldHandleInstructMode()
        {
            var expectedResponse = new ChatCompletionResponse
            {
                Id = "test",
                Object = "chat.completion",
                Created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Model = "test-model",
                Choices = new List<ChatCompletionChoice>
                {
                    new()
                    {
                        Index = 0,
                        Message = new ChatMessage
                        {
                            Role = "assistant",
                            Content = "Response"
                        },
                        FinishReason = "stop"
                    }
                }
            };

            Server
                .Given(Request.Create()
                    .WithPath("/v1/chat/completions")
                    .WithBody(body => body.Contains("\"mode\":\"instruct\""))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithHeader("Content-Type", "application/json")
                    .WithBody(JsonSerializer.Serialize(expectedResponse)));

            var response = await Client.ChatCompleteAsync(
                new List<ChatMessage> { new() { Role = "user", Content = "Test" } },
                new ChatCompletionOptions { Mode = "instruct" });

            response.Should().NotBeNull();
            response.Choices.Should().NotBeEmpty();
            response.Choices[0].Message.Content.Should().Be("Response");
        }

        [Fact]
        public async Task IsAvailableAsync_WhenServerResponds_ShouldReturnTrue()
        {
            // Arrange
            Server
                .Given(Request.Create()
                    .WithPath("/v1/models")
                    .UsingGet())
                .RespondWith(Response.Create()
                    .WithStatusCode(200));

            // Act
            var isAvailable = await Client.IsAvailableAsync();

            // Assert
            isAvailable.Should().BeTrue();
        }
    }
}