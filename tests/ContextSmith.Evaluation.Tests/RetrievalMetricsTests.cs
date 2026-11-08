using ContextSmith.Evaluation;

namespace ContextSmith.Evaluation.Tests;

// Expected values are worked out by hand from the metric definitions, not recomputed with the same formula.
public class RetrievalMetricsTests
{
    [Fact]
    public void RecallAtK_counts_relevant_results_in_the_top_k_over_all_relevant_chunks()
    {
        // Ranked relevance: R N R N. Two of the three relevant chunks sit in the top 3 -> 2/3.
        bool[] ranked = [true, false, true, false];

        Assert.Equal(2.0 / 3.0, RetrievalMetrics.RecallAtK(ranked, totalRelevant: 3, k: 3), precision: 10);
        Assert.Equal(1.0 / 3.0, RetrievalMetrics.RecallAtK(ranked, totalRelevant: 3, k: 1), precision: 10);
    }

    [Fact]
    public void RecallAtK_is_zero_when_nothing_is_relevant()
    {
        Assert.Equal(0.0, RetrievalMetrics.RecallAtK([false, false], totalRelevant: 0, k: 2));
    }

    [Theory]
    [InlineData(new[] { true, false, false }, 1.0)]
    [InlineData(new[] { false, false, true }, 1.0 / 3.0)]
    [InlineData(new[] { false, false, false }, 0.0)]
    public void ReciprocalRank_is_one_over_the_rank_of_the_first_relevant_result(bool[] ranked, double expected)
    {
        Assert.Equal(expected, RetrievalMetrics.ReciprocalRank(ranked), precision: 10);
    }

    [Fact]
    public void NdcgAtK_is_one_for_a_perfect_ranking()
    {
        Assert.Equal(1.0, RetrievalMetrics.NdcgAtK([true, true, false], totalRelevant: 2, k: 3), precision: 10);
    }

    [Fact]
    public void NdcgAtK_discounts_a_relevant_result_that_ranks_lower()
    {
        // Ranking N R: DCG = 1/log2(3) = 0.63093. Ideal (one relevant chunk): 1/log2(2) = 1.
        Assert.Equal(0.6309297535714574, RetrievalMetrics.NdcgAtK([false, true], totalRelevant: 1, k: 2), precision: 10);
    }
}
