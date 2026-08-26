namespace ContextSmith.Application;

public interface IRetrievalService
{
    /// <summary>
    /// Inserts <paramref name="chunk"/>, or replaces the entry already indexed under the same
    /// <see cref="Chunk.Id"/>, so re-indexing a chunk never produces a duplicate.
    /// </summary>
    Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Chunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
}
