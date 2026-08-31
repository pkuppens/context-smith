using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ContextSmith.Application;

namespace ContextSmith.Retrieval.Local;

/// <summary><see cref="IEmbeddingService"/> that calls a local Ollama server's <c>api/embeddings</c> endpoint.</summary>
/// <param name="httpClient">Client pointed at the Ollama base address.</param>
/// <param name="model">Ollama embedding model name.</param>
public sealed class OllamaEmbeddingService(HttpClient httpClient, string model = "nomic-embed-text") : IEmbeddingService
{
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Ollama returned no embedding.</exception>
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
