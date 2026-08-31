using System.Collections.Concurrent;
using ContextSmith.Application;

namespace ContextSmith.Retrieval.Local;

/// <summary>Process-lifetime <see cref="IRetrievalService"/> that holds chunk embeddings in memory and ranks them by cosine similarity.</summary>
public sealed class InMemoryRetrievalService : IRetrievalService
{
    private readonly ConcurrentDictionary<string, (Chunk Chunk, float[] Embedding)> _index = new();

    /// <inheritdoc/>
    public Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default)
    {
        _index[chunk.Id] = (chunk, embedding);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Chunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Chunk> results = _index.Values
            .Select(entry => (entry.Chunk, Score: CosineSimilarity(queryEmbedding, entry.Embedding)))
            .OrderByDescending(entry => entry.Score)
            .Take(topK)
            .Select(entry => entry.Chunk)
            .ToList();

        return Task.FromResult(results);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
        {
            throw new ArgumentException("Embeddings must have the same length to compare.");
        }

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA == 0 || normB == 0)
        {
            return 0f;
        }

        return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
    }
}
