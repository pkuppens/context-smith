namespace ContextSmith.Application;

/// <summary>Turns text into an embedding vector for similarity search.</summary>
public interface IEmbeddingService
{
    /// <summary>Computes the embedding vector for <paramref name="text"/>.</summary>
    /// <param name="text">Text to embed.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    /// <returns>The embedding vector.</returns>
    Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default);
}
