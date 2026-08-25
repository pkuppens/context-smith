using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContextSmith.Application;

namespace ContextSmith.Retrieval.Local;

public sealed class OllamaEmbeddingService(HttpClient httpClient, string model = "nomic-embed-text") : IEmbeddingService
{
    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var response = await httpClient
            .PostAsJsonAsync("api/embeddings", new OllamaEmbeddingRequest(model, text), cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken)
            .ConfigureAwait(false);

        return result?.Embedding
            ?? throw new InvalidOperationException("Ollama returned no embedding.");
    }

    private sealed record OllamaEmbeddingRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt);

    private sealed record OllamaEmbeddingResponse(
        [property: JsonPropertyName("embedding")] float[] Embedding);
}
