using System.Text.RegularExpressions;
using ContextSmith.Application;

namespace ContextSmith.Evaluation;

/// <summary>
/// Offline, deterministic bag-of-words embedding: each word adds one count to a hashed bucket.
/// It needs no model or network, so evaluation runs give the same numbers on every machine.
/// </summary>
public sealed partial class HashingEmbeddingService : IEmbeddingService
{
    private const int Dimension = 512;

    /// <inheritdoc/>
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var vector = new float[Dimension];
        foreach (Match word in WordPattern().Matches(text.ToLowerInvariant()))
        {
            vector[(int)(Fnv1a(word.Value) % Dimension)] += 1;
        }

        return Task.FromResult(vector);
    }

    // string.GetHashCode is randomized per process, so use a fixed hash for repeatable runs.
    private static uint Fnv1a(string value)
    {
        var hash = 2166136261u;
        foreach (var ch in value)
        {
            hash = (hash ^ ch) * 16777619u;
        }

        return hash;
    }

    [GeneratedRegex(@"\w+")]
    private static partial Regex WordPattern();
}
