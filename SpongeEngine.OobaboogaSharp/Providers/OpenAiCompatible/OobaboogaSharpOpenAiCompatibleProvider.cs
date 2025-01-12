using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Common;
using SpongeEngine.OobaboogaSharp.Models.Completion;
using Exception = SpongeEngine.OobaboogaSharp.Models.Common.Exception;
using JsonException = Newtonsoft.Json.JsonException;

namespace SpongeEngine.OobaboogaSharp.Providers.OpenAiCompatible
{
    public class OobaboogaSharpOpenAiCompatibleProvider : IOobaboogaSharpOpenAiCompatibleProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger? _logger;
        private readonly JsonSerializerSettings? _jsonSettings;
        private readonly string _modelName;
        private bool _disposed;

        public OobaboogaSharpOpenAiCompatibleProvider(
            HttpClient httpClient,
            string modelName = "default",
            ILogger? logger = null,
            JsonSerializerSettings? jsonSettings = null)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _modelName = modelName;
            _logger = logger;
            _jsonSettings = jsonSettings;
        }

        public async Task<string> CompleteAsync(
            string prompt,
            CompletionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = options?.ModelName ?? _modelName,
                prompt = prompt,
                max_tokens = options?.MaxTokens ?? 80,
                temperature = options?.Temperature ?? 0.7f,
                top_p = options?.TopP ?? 0.9f,
                stop = options?.StopSequences,
                stream = false
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(request, _jsonSettings),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync("v1/completions", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    "Completion request failed",
                    "Oobabooga",
                    (int)response.StatusCode,
                    responseContent);
            }

            var result = JsonConvert.DeserializeObject<CompletionResponse>(responseContent, _jsonSettings);
            return result?.Choices.FirstOrDefault()?.Text ?? string.Empty;
        }

        public async Task<ChatCompletionResponse> ChatCompleteAsync(
            List<ChatMessage> messages,
            ChatCompletionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (!messages.Any())
            {
                throw new ArgumentException("Messages cannot be empty", nameof(messages));
            }
            
            ChatCompletionRequest request = new ChatCompletionRequest
            {
                Model = options?.ModelName ?? _modelName,
                Messages = messages,
                Temperature = options?.Temperature,
                TopP = options?.TopP,
                MaxTokens = options?.MaxTokens,
                StopSequences = options?.StopSequences,
                Mode = options?.Mode ?? "chat",
                InstructionTemplate = options?.InstructionTemplate,
                Character = options?.Character,
                Stream = false
            };

            StringContent content = new StringContent(JsonConvert.SerializeObject(request, _jsonSettings), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.PostAsync("v1/chat/completions", content, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    "Chat completion request failed", 
                    "Oobabooga",
                    (int)response.StatusCode,
                    responseContent);
            }
            
            ChatCompletionResponse? result = JsonConvert.DeserializeObject<ChatCompletionResponse>(responseContent, _jsonSettings);
            return result ?? new ChatCompletionResponse();
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(
            string prompt,
            CompletionOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = options?.ModelName ?? _modelName,
                prompt = prompt,
                max_tokens = options?.MaxTokens ?? 80,
                temperature = options?.Temperature ?? 0.7f,
                top_p = options?.TopP ?? 0.9f,
                stop = options?.StopSequences,
                stream = true
            };

            var requestJson = JsonConvert.SerializeObject(request, _jsonSettings);
            _logger?.LogDebug("Streaming completion request: {Payload}", requestJson);

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/completions")
            {
                Content = content
            };

            using var response = await _httpClient.SendAsync(httpRequest, 
                HttpCompletionOption.ResponseHeadersRead, 
                cancellationToken);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                    continue;

                var data = line[6..];
                if (data == "[DONE]")
                    break;

                StreamCompletion? completion = null;
                try 
                {
                    completion = JsonConvert.DeserializeObject<StreamCompletion>(data, _jsonSettings);
                }
                catch (JsonException ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse SSE message: {Message}", data);
                    continue;
                }

                var text = completion?.Choices?.FirstOrDefault()?.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    yield return text;
                }
            }
        }
        
        public class ChatCompletionChunkDelta
        {
            [JsonProperty("role")]
            public string? Role { get; set; }

            [JsonProperty("content")]
            public string? Content { get; set; }
        }

        public class ChatCompletionChunkChoice 
        {
            [JsonProperty("index")]
            public int Index { get; set; }

            [JsonProperty("finish_reason")]
            public string? FinishReason { get; set; }

            [JsonProperty("delta")]
            public ChatCompletionChunkDelta? Delta { get; set; }
        }

        public class ChatCompletionChunkResponse
        {
            [JsonProperty("id")]
            public string Id { get; set; } = string.Empty;

            [JsonProperty("object")]
            public string Object { get; set; } = "chat.completion.chunk";

            [JsonProperty("created")]
            public long Created { get; set; }

            [JsonProperty("model")] 
            public string Model { get; set; } = string.Empty;

            [JsonProperty("choices")]
            public List<ChatCompletionChunkChoice> Choices { get; set; } = new();
        }

        public async IAsyncEnumerable<ChatMessage> StreamChatCompletionAsync(
            List<ChatMessage> messages,
            ChatCompletionOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var httpResponse = await GetStreamResponseAsync(messages, options, cancellationToken);
            using var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);
            
            string? currentRole = null;
            bool firstDelta = true;

            while (!reader.EndOfStream)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                    continue;

                var data = line[6..];  // Skip "data: " prefix
                if (data == "[DONE]")
                    break;

                ChatCompletionChunkResponse? chunk;
                try 
                {
                    chunk = JsonConvert.DeserializeObject<ChatCompletionChunkResponse>(data, _jsonSettings);
                }
                catch (JsonException ex)
                {
                    _logger?.LogWarning(ex, "Failed to parse streaming response: {Data}", data);
                    continue;
                }

                var delta = chunk?.Choices?.FirstOrDefault()?.Delta;
                if (delta == null) 
                    continue;

                // Handle first delta which usually just sets the role
                if (firstDelta)
                {
                    firstDelta = false;
                    if (!string.IsNullOrEmpty(delta.Role))
                    {
                        currentRole = delta.Role;
                        continue;
                    }
                }

                // Update role if provided
                if (!string.IsNullOrEmpty(delta.Role))
                {
                    currentRole = delta.Role;
                }

                // Only yield messages with actual content
                if (!string.IsNullOrWhiteSpace(delta.Content))
                {
                    yield return new ChatMessage
                    {
                        Role = currentRole ?? "assistant",
                        Content = delta.Content
                    };
                }
            }
        }
        
        private async Task<HttpResponseMessage> GetStreamResponseAsync(
            List<ChatMessage> messages,
            ChatCompletionOptions? options,
            CancellationToken cancellationToken)
        {
            try
            {
                var request = CreateStreamRequest(messages, options);
                var response = await _httpClient.SendAsync(request, 
                    HttpCompletionOption.ResponseHeadersRead, 
                    cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new Exception(
                        "Stream chat completion request failed",
                        "Oobabooga",
                        (int)response.StatusCode,
                        errorContent);
                }

                return response;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception(
                    "Stream chat completion request failed",
                    "Oobabooga",
                    ex.StatusCode?.GetHashCode() ?? 500,
                    ex.Message);
            }
        }
        
        private HttpRequestMessage CreateStreamRequest(List<ChatMessage> messages, ChatCompletionOptions? options)
        {
            var request = new ChatCompletionRequest
            {
                Model = options?.ModelName ?? _modelName,
                Messages = messages,
                Temperature = options?.Temperature,
                TopP = options?.TopP,
                MaxTokens = options?.MaxTokens,
                StopSequences = options?.StopSequences,
                Mode = options?.Mode ?? "chat",
                InstructionTemplate = options?.InstructionTemplate,
                Character = options?.Character,
                Stream = true  // Make sure this is set
            };

            _logger?.LogDebug("Sending request: {Request}", 
                JsonConvert.SerializeObject(request, Formatting.Indented));

            return new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(
                    JsonConvert.SerializeObject(request, _jsonSettings),
                    Encoding.UTF8,
                    "application/json"),
                Headers = 
                {
                    Accept = { new MediaTypeWithQualityHeaderValue("text/event-stream") }
                }
            };
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var response = await _httpClient.GetAsync("v1/models", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Don't dispose the HttpClient as it was injected
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private class StreamCompletion
        {
            [JsonProperty("choices")]
            public List<Choice> Choices { get; set; } = new();

            public class Choice
            {
                [JsonProperty("text")]
                public string Text { get; set; } = string.Empty;
            }
        }
    }
}