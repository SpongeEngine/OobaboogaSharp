using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SpongeEngine.OobaboogaSharp.Utils
{
    public static class StreamUtils 
    {
        public static async IAsyncEnumerable<T> ParseSseStream<T>(
            StreamReader reader,
            ILogger? logger = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            while (!reader.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await reader.ReadLineAsync();
                if (string.IsNullOrEmpty(line) || !line.StartsWith("data: "))
                    continue;

                var data = line[6..];
                if (data == "[DONE]")
                    break;

                T? result = default;
                try 
                {
                    result = JsonSerializer.Deserialize<T>(data);
                }
                catch (JsonException ex)
                {
                    logger?.LogWarning(ex, "Failed to parse SSE message: {Message}", data);
                    continue;
                }

                if (result != null)
                    yield return result;
            }
        }
    }
}