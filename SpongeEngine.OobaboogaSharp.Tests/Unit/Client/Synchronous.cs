using FluentAssertions;
using Newtonsoft.Json;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Common;
using SpongeEngine.OobaboogaSharp.Tests.Common;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using Xunit;
using Xunit.Abstractions;
using Exception = SpongeEngine.OobaboogaSharp.Models.Common.Exception;

namespace SpongeEngine.OobaboogaSharp.Tests.Unit.Client
{
    public class Synchronous : UnitTestBase
    {
        private readonly OobaboogaSharpClient _clientOobaboogaSharpClient;

        public Synchronous(ITestOutputHelper output) : base(output)
        {
            _clientOobaboogaSharpClient = new OobaboogaSharpClient(new Options
            {
                BaseUrl = BaseUrl
            }, Logger);
        }

        [Fact]
        public async Task Complete_WithBasicPrompt_ShouldWork()
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
            var response = await _clientOobaboogaSharpClient.CompleteAsync("Test prompt");

            // Assert
            response.Should().Be(expectedResponse);
        }

        [Fact]
        public async Task ChatComplete_WithCharacter_ShouldWork()
        {
            // Arrange
            var expectedResponse = new ChatCompletionResponse
            {
                Id = "test",
                Choices = new List<ChatCompletionChoice>
                {
                    new()
                    {
                        Message = new ChatMessage { Role = "assistant", Content = "Hello!" }
                    }
                }
            };

            Server
                .Given(Request.Create()
                    .WithPath("/v1/chat/completions")
                    .WithBody(body => body.Contains("\"character\":\"Example\""))
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody(JsonConvert.SerializeObject(expectedResponse)));

            // Act
            var response = await _clientOobaboogaSharpClient.ChatCompleteAsync(
                new List<ChatMessage> { new() { Role = "user", Content = "Hi" } },
                new ChatCompletionOptions { Character = "Example" });

            // Assert
            response.Choices[0].Message.Content.Should().Be("Hello!");
        }

        [Fact]
        public async Task Complete_WithError_ShouldThrowException()
        {
            // Arrange
            Server
                .Given(Request.Create()
                    .WithPath("/v1/completions")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(500)
                    .WithBody("Internal server error"));

            // Act & Assert
            var act = () => _clientOobaboogaSharpClient.CompleteAsync("Test prompt");
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Completion request failed");
        }

        [Fact]
        public async Task Complete_WithCancellation_ShouldCancel()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            Server
                .Given(Request.Create()
                    .WithPath("/v1/completions")
                    .UsingPost())
                .RespondWith(Response.Create()
                    .WithStatusCode(200)
                    .WithBody("...")
                    .WithDelay(TimeSpan.FromSeconds(5)));

            // Act & Assert
            var completeTask = _clientOobaboogaSharpClient.CompleteAsync("Test prompt", cancellationToken: cts.Token);
            cts.Cancel();
    
            await Assert.ThrowsAsync<TaskCanceledException>(() => completeTask);
        }
    }
}