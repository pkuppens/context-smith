using ContextSmith.Application;
using ContextSmith.Evaluation;

namespace ContextSmith.Evaluation.Tests;

public class EvaluationRunnerTests
{
    private const string Handbook = """
        # Handbook

        ## Leave Policy

        Parental leave starts after twelve months of employment.

        ## Parking

        Office parking is first come first served.
        """;

    [Fact]
    public async Task RunAsync_scores_a_strategy_that_finds_the_judged_chunk_first_with_perfect_metrics()
    {
        var dataset = new EvaluationDataset(
            [new EvaluationDocument("handbook.md", Handbook)],
            [new EvaluationQuery("leave", "When does parental leave start?", ["twelve months of employment"])]);

        var results = await new EvaluationRunner().RunAsync(
            dataset,
            new Dictionary<string, IChunkingStrategy> { ["structure"] = new StructureAwareChunker() },
            k: 1);

        var result = Assert.Single(results);
        Assert.Equal("structure", result.Strategy);
        Assert.Equal(1.0, result.RecallAtK);
        Assert.Equal(1.0, result.MeanReciprocalRank);
        Assert.Equal(1.0, result.Ndcg);
    }

    [Fact]
    public async Task RunAsync_scores_zero_when_no_chunk_contains_a_judged_phrase()
    {
        var dataset = new EvaluationDataset(
            [new EvaluationDocument("handbook.md", Handbook)],
            [new EvaluationQuery("leave", "When does parental leave start?", ["a phrase the documents never use"])]);

        var result = Assert.Single(await new EvaluationRunner().RunAsync(
            dataset,
            new Dictionary<string, IChunkingStrategy> { ["fixed"] = new FixedSizeChunker(200) },
            k: 3));

        Assert.Equal(0.0, result.RecallAtK);
        Assert.Equal(0.0, result.MeanReciprocalRank);
        Assert.Equal(0.0, result.Ndcg);
    }
}
