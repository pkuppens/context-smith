namespace ContextSmith.Evaluation;

/// <summary>
/// Rank-based retrieval metrics with binary relevance. Each method takes the relevance of the
/// ranked results (best first), where <see langword="true"/> means the result is relevant.
/// </summary>
public static class RetrievalMetrics
{
    /// <summary>Share of all relevant chunks that appear in the top <paramref name="k"/> results.</summary>
    /// <param name="ranked">Relevance of the ranked results, best first.</param>
    /// <param name="totalRelevant">Number of relevant chunks in the whole corpus.</param>
    /// <param name="k">Cut-off rank.</param>
    /// <returns>A value from 0 to 1. Zero when <paramref name="totalRelevant"/> is zero.</returns>
    public static double RecallAtK(IReadOnlyList<bool> ranked, int totalRelevant, int k)
        => totalRelevant == 0 ? 0 : (double)ranked.Take(k).Count(relevant => relevant) / totalRelevant;

    /// <summary>One divided by the rank of the first relevant result.</summary>
    /// <param name="ranked">Relevance of the ranked results, best first.</param>
    /// <returns>A value from 0 to 1. Zero when no result is relevant.</returns>
    public static double ReciprocalRank(IReadOnlyList<bool> ranked)
    {
        for (var i = 0; i < ranked.Count; i++)
        {
            if (ranked[i])
            {
                return 1.0 / (i + 1);
            }
        }

        return 0;
    }

    /// <summary>Normalized discounted cumulative gain at <paramref name="k"/>, with gain 1 for a relevant result.</summary>
    /// <param name="ranked">Relevance of the ranked results, best first.</param>
    /// <param name="totalRelevant">Number of relevant chunks in the whole corpus. Sets the ideal ranking.</param>
    /// <param name="k">Cut-off rank.</param>
    /// <returns>A value from 0 to 1. Zero when <paramref name="totalRelevant"/> is zero.</returns>
    public static double NdcgAtK(IReadOnlyList<bool> ranked, int totalRelevant, int k)
    {
        var dcg = ranked.Take(k).Select((relevant, i) => relevant ? Discount(i) : 0).Sum();
        var ideal = Enumerable.Range(0, Math.Min(k, totalRelevant)).Select(Discount).Sum();
        return ideal == 0 ? 0 : dcg / ideal;
    }

    private static double Discount(int zeroBasedRank) => 1.0 / Math.Log2(zeroBasedRank + 2);
}
