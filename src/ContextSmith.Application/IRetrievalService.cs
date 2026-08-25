namespace ContextSmith.Application;

public interface IRetrievalService
{
    Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Chunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
}
