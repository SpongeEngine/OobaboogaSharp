using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Common;
using SpongeEngine.OobaboogaSharp.Models.Completion;
using SpongeEngine.OobaboogaSharp.Providers.OpenAiCompatible;

namespace SpongeEngine.OobaboogaSharp
{
    public class OobaboogaSharpClient : IDisposable
    {
        private readonly IOobaboogaSharpOpenAiCompatibleProvider _oobaboogaSharpOpenAiCompatibleProvider;
        private readonly Options _options;
        private bool _disposed;

        public string Name { get; set; }
        public string? Version { get; private set; }
        public bool SupportsStreaming => true;

        public OobaboogaSharpClient(Options options, ILogger? logger = null, JsonSerializerSettings? jsonSettings = null)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            JsonSerializerSettings settings = jsonSettings ?? new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            HttpClient httpClient = new HttpClient
            {
                BaseAddress = new Uri(options.BaseUrl),
                Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds)
            };

            httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/event-stream"));

            if (!string.IsNullOrEmpty(options.ApiKey))
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
            }

            _oobaboogaSharpOpenAiCompatibleProvider = new OobaboogaSharpOpenAiCompatibleProvider(httpClient, logger: logger, jsonSettings: settings);
        }

        // Text completion methods
        public Task<string> CompleteAsync(string prompt, CompletionOptions? options = null, CancellationToken cancellationToken = default)
        {
            return _oobaboogaSharpOpenAiCompatibleProvider.CompleteAsync(prompt, options, cancellationToken);
        }

        public IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CompletionOptions? options = null, CancellationToken cancellationToken = default)
        {
            return _oobaboogaSharpOpenAiCompatibleProvider.StreamCompletionAsync(prompt, options, cancellationToken);
        }

        // Chat completion methods
        public Task<ChatCompletionResponse> ChatCompleteAsync(
            List<ChatMessage> messages, 
            ChatCompletionOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            return _oobaboogaSharpOpenAiCompatibleProvider.ChatCompleteAsync(messages, options, cancellationToken);
        }

        public IAsyncEnumerable<ChatMessage> StreamChatCompletionAsync(
            List<ChatMessage> messages, 
            ChatCompletionOptions? options = null, 
            CancellationToken cancellationToken = default)
        {
            return _oobaboogaSharpOpenAiCompatibleProvider.StreamChatCompletionAsync(messages, options, cancellationToken);
        }

        // Health check method
        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return _oobaboogaSharpOpenAiCompatibleProvider.IsAvailableAsync(cancellationToken);
        }

        // Helper method to create a chat message
        public static ChatMessage CreateChatMessage(string role, string content)
        {
            return new ChatMessage
            {
                Role = role,
                Content = content
            };
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _oobaboogaSharpOpenAiCompatibleProvider?.Dispose();
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}