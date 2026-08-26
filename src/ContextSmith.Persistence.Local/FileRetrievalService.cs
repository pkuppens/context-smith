using System.Collections.Concurrent;
using System.Text.Json;
using ContextSmith.Application;

namespace ContextSmith.Persistence.Local;

public sealed class FileRetrievalService : IRetrievalService
{
    private readonly string _indexDirectory;
    private readonly string _indexPath;
    private readonly ConcurrentDictionary<string, IndexEntry> _index;

    public FileRetrievalService(string rootDirectory, string documentId)
    {
        _indexDirectory = Path.Combine(rootDirectory, "retrieval");
        _indexPath = Path.Combine(_indexDirectory, $"{documentId}.json");

        var entries = File.Exists(_indexPath)
            ? JsonSerializer.Deserialize<List<IndexEntry>>(File.ReadAllText(_indexPath)) ?? []
            : [];

        _index = new ConcurrentDictionary<string, IndexEntry>(entries.ToDictionary(entry => entry.Chunk.Id));
    }

    public Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default)
    {
        _index[chunk.Id] = new IndexEntry(chunk, embedding);
        Save();
        return Task.CompletedTask;
    }

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

    private void Save()
    {
        Directory.CreateDirectory(_indexDirectory);
        File.WriteAllText(_indexPath, JsonSerializer.Serialize(_index.Values.ToList()));
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

    private sealed record IndexEntry(Chunk Chunk, float[] Embedding);
}
