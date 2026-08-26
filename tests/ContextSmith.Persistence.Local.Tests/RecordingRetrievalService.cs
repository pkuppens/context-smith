using ContextSmith.Application;

namespace ContextSmith.Persistence.Local.Tests;

/// <summary>A no-op <see cref="IRetrievalService"/> that records calls instead of touching real storage.</summary>
public sealed class RecordingRetrievalService : IRetrievalService
{
    public List<(Chunk Chunk, float[] Embedding)> IndexCalls { get; } = [];

    public Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default)
    {
        IndexCalls.Add((chunk, embedding));
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Chunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Chunk>>([]);
}
