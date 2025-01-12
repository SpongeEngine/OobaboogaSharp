using SpongeEngine.OobaboogaSharp.Models.Chat;
using SpongeEngine.OobaboogaSharp.Models.Completion;

namespace SpongeEngine.OobaboogaSharp.Providers.OpenAiCompatible
{
    public interface IOobaboogaSharpOpenAiCompatibleProvider : IDisposable
    {
        Task<string> CompleteAsync(string prompt, CompletionOptions? options = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<string> StreamCompletionAsync(string prompt, CompletionOptions? options = null, CancellationToken cancellationToken = default);
        Task<ChatCompletionResponse> ChatCompleteAsync(List<ChatMessage> messages, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default);
        IAsyncEnumerable<ChatMessage> StreamChatCompletionAsync(List<ChatMessage> messages, ChatCompletionOptions? options = null, CancellationToken cancellationToken = default);
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    }
}