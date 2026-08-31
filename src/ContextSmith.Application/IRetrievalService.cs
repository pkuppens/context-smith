namespace ContextSmith.Application;

public interface IRetrievalService
{
    /// <summary>
    /// Inserts <paramref name="chunk"/>, or replaces the entry already indexed under the same
    /// <see cref="Chunk.Id"/>, so re-indexing a chunk never produces a duplicate.
    /// </summary>
    Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the <paramref name="topK"/> indexed chunks whose embeddings are most similar to
    /// <paramref name="queryEmbedding"/>, ordered from most to least similar.
    /// </summary>
    Task<IReadOnlyList<Chunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
}
