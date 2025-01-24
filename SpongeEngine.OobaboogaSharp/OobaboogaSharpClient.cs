using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using SpongeEngine.LLMSharp.Core;
using SpongeEngine.LLMSharp.Core.Interfaces;
using SpongeEngine.LLMSharp.Core.Models;
using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Completion;
using ChatMessage = SpongeEngine.OobaboogaSharp.Models.Chat.ChatMessage;
using CompletionOptions = SpongeEngine.OobaboogaSharp.Models.Completion.CompletionOptions;
using CompletionRequest = SpongeEngine.LLMSharp.Core.Models.CompletionRequest;
using Exception = SpongeEngine.OobaboogaSharp.Models.Common.Exception;

namespace SpongeEngine.OobaboogaSharp
{
    public class OobaboogaSharpClient: LlmClientBase, ICompletionService
    {
        public OobaboogaSharpClient(OobaboogaSharpClientOptions options): base(options) {}

        public async Task<string> CompleteAsync(
            string prompt,
            CompletionOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = options?.ModelName,
                prompt = prompt,
                max_tokens = options?.MaxTokens ?? 80,
                temperature = options?.Temperature ?? 0.7f,
                top_p = options?.TopP ?? 0.9f,
                stop = options?.StopSequences,
                stream = false
            };

            var content = new StringContent(
                JsonSerializer.Serialize(request, Options.JsonSerializerOptions),
                Encoding.UTF8,
                "application/json");

            var response = await Options.HttpClient.PostAsync("v1/completions", content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    "Completion request failed",
                    "Oobabooga",
                    (int)response.StatusCode,
                    responseContent);
            }

            var result = JsonSerializer.Deserialize<CompletionResponse>(responseContent, Options.JsonSerializerOptions);
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
                Model = options?.ModelName,
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

            StringContent content = new StringContent(JsonSerializer.Serialize(request, Options.JsonSerializerOptions), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await Options.HttpClient.PostAsync("v1/chat/completions", content, cancellationToken);
            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    "Chat completion request failed", 
                    "Oobabooga",
                    (int)response.StatusCode,
                    responseContent);
            }
            
            ChatCompletionResponse? result = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, Options.JsonSerializerOptions);
            return result ?? new ChatCompletionResponse();
        }

        public async IAsyncEnumerable<string> StreamCompletionAsync(
            string prompt,
            CompletionOptions? options = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var request = new
            {
                model = options?.ModelName,
                prompt = prompt,
                max_tokens = options?.MaxTokens ?? 80,
                temperature = options?.Temperature ?? 0.7f,
                top_p = options?.TopP ?? 0.9f,
                stop = options?.StopSequences,
                stream = true
            };

            var requestJson = JsonSerializer.Serialize(request, Options.JsonSerializerOptions);
            Options.Logger?.LogDebug("Streaming completion request: {Payload}", requestJson);

            var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            
            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/completions")
            {
                Content = content
            };

            using var response = await Options.HttpClient.SendAsync(httpRequest, 
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
                    completion = JsonSerializer.Deserialize<StreamCompletion>(data, Options.JsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    Options.Logger?.LogWarning(ex, "Failed to parse SSE message: {Message}", data);
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
            [JsonPropertyName("role")]
            public string? Role { get; set; }

            [JsonPropertyName("content")]
            public string? Content { get; set; }
        }

        public class ChatCompletionChunkChoice 
        {
            [JsonPropertyName("index")]
            public int Index { get; set; }

            [JsonPropertyName("finish_reason")]
            public string? FinishReason { get; set; }

            [JsonPropertyName("delta")]
            public ChatCompletionChunkDelta? Delta { get; set; }
        }

        public class ChatCompletionChunkResponse
        {
            [JsonPropertyName("id")]
            public string Id { get; set; } = string.Empty;

            [JsonPropertyName("object")]
            public string Object { get; set; } = "chat.completion.chunk";

            [JsonPropertyName("created")]
            public long Created { get; set; }

            [JsonPropertyName("model")] 
            public string Model { get; set; } = string.Empty;

            [JsonPropertyName("choices")]
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
                    chunk = JsonSerializer.Deserialize<ChatCompletionChunkResponse>(data, Options.JsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    Options.Logger?.LogWarning(ex, "Failed to parse streaming response: {Data}", data);
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
                var response = await Options.HttpClient.SendAsync(request, 
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
                Model = options?.ModelName,
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

            Options.Logger?.LogDebug("Sending request: {Request}", JsonSerializer.Serialize(request, Options.JsonSerializerOptions));

            return new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(request, Options.JsonSerializerOptions),
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
                var response = await Options.HttpClient.GetAsync("v1/models", cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private class StreamCompletion
        {
            [JsonPropertyName("choices")]
            public List<Choice> Choices { get; set; } = new();

            public class Choice
            {
                [JsonPropertyName("text")]
                public string Text { get; set; } = string.Empty;
            }
        }

        public async Task<CompletionResult> CompleteAsync(
            CompletionRequest request,
            CancellationToken cancellationToken = default)
        {
            var startTime = DateTime.UtcNow;
    
            var options = new Models.Completion.CompletionOptions
            {
                ModelName = request.ModelId,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                TopP = request.TopP,
                StopSequences = request.StopSequences.ToArray()
            };

            // Apply any provider-specific parameters
            foreach (var param in request.ProviderParameters)
            {
                Options.Logger?.LogDebug("Additional provider parameter: {Key}={Value}", param.Key, param.Value);
                // Here we could handle specific Oobabooga parameters if needed
            }

            var response = await CompleteAsync(request.Prompt, options, cancellationToken);
            var generationTime = DateTime.UtcNow - startTime;

            return new CompletionResult
            {
                Text = response,
                ModelId = request.ModelId,
                TokenUsage = new CompletionTokenUsage
                {
                    // Note: Oobabooga API doesn't provide token usage information
                    // These would need to be computed using a tokenizer if needed
                    PromptTokens = 0,
                    CompletionTokens = 0,
                    TotalTokens = 0
                },
                FinishReason = null, // Oobabooga doesn't provide finish reason in non-streaming mode
                GenerationTime = generationTime,
                Metadata = new Dictionary<string, object>
                {
                    ["provider"] = "Oobabooga",
                    ["sourceUrl"] = Options.HttpClient.BaseAddress,
                }
            };
        }

        public async IAsyncEnumerable<CompletionToken> StreamCompletionAsync(
            CompletionRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var options = new Models.Completion.CompletionOptions
            {
                ModelName = request.ModelId,
                MaxTokens = request.MaxTokens,
                Temperature = request.Temperature,
                TopP = request.TopP,
                StopSequences = request.StopSequences.ToArray()
            };

            var totalTokens = 0;
            string lastToken = string.Empty;

            await foreach (var token in StreamCompletionAsync(request.Prompt, options, cancellationToken))
            {
                // Simple token count estimation
                var tokenCount = EstimateTokenCount(token);
                totalTokens += tokenCount;

                lastToken = token;

                yield return new CompletionToken
                {
                    Text = token,
                    TokenCount = totalTokens,
                    FinishReason = null // Set when we detect end of stream or stop sequence
                };
            }

            // Set finish reason for the last token if we can determine it
            if (request.StopSequences.Any(stop => lastToken.EndsWith(stop)))
            {
                yield return new CompletionToken
                {
                    Text = string.Empty,
                    TokenCount = totalTokens,
                    FinishReason = "stop"
                };
            }
            else if (options.MaxTokens.HasValue && totalTokens >= options.MaxTokens.Value)
            {
                yield return new CompletionToken
                {
                    Text = string.Empty,
                    TokenCount = totalTokens,
                    FinishReason = "length"
                };
            }
            else
            {
                yield return new CompletionToken
                {
                    Text = string.Empty,
                    TokenCount = totalTokens,
                    FinishReason = "done"
                };
            }
        }
        
        private static int EstimateTokenCount(string text)
        {
            // This is a very rough estimation - in practice you'd want to use
            // a proper tokenizer that matches your model
            if (string.IsNullOrEmpty(text)) return 0;
    
            // Split on whitespace and punctuation
            var tokens = text.Split(new[] { ' ', '\n', '\r', '\t', '.', ',', '!', '?', ';', ':' }, 
                StringSplitOptions.RemoveEmptyEntries);
    
            // Count remaining non-whitespace characters as potential sub-word tokens
            var remainingChars = text.Count(c => !char.IsWhiteSpace(c));
            var estimatedSubwordTokens = remainingChars / 4; // Assume average subword length of 4
    
            return Math.Max(1, tokens.Length + estimatedSubwordTokens);
        }
    }
}