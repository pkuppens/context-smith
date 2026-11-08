using System.Text;
using ContextSmith.Application;
using ContextSmith.Documents.Text;
using ContextSmith.Domain;
using ContextSmith.Retrieval.Local;

namespace ContextSmith.Evaluation;

/// <summary>Metric values for one chunking strategy, averaged over all queries.</summary>
/// <param name="Strategy">Strategy name.</param>
/// <param name="ChunkCount">Chunks the strategy produced for the dataset.</param>
/// <param name="K">Cut-off rank used for Recall@K and nDCG@K.</param>
/// <param name="RecallAtK">Mean Recall@K.</param>
/// <param name="MeanReciprocalRank">Mean reciprocal rank (MRR).</param>
/// <param name="Ndcg">Mean nDCG@K.</param>
public sealed record StrategyResult(string Strategy, int ChunkCount, int K, double RecallAtK, double MeanReciprocalRank, double Ndcg);

/// <summary>Runs a judged query set against each chunking strategy and scores the ranked results.</summary>
public sealed class EvaluationRunner
{
    private readonly IEmbeddingService _embeddingService;

    /// <summary>Creates a runner that embeds with <see cref="HashingEmbeddingService"/>.</summary>
    public EvaluationRunner()
        : this(new HashingEmbeddingService())
    {
    }

    /// <summary>Creates a runner with a chosen embedding service.</summary>
    /// <param name="embeddingService">Embedding service for chunks and queries.</param>
    public EvaluationRunner(IEmbeddingService embeddingService) => _embeddingService = embeddingService;

    /// <summary>Scores each strategy on <paramref name="dataset"/>.</summary>
    /// <param name="dataset">Documents and judged queries.</param>
    /// <param name="strategies">Chunking strategies by name.</param>
    /// <param name="k">Cut-off rank. Retrieval returns the top <paramref name="k"/> chunks.</param>
    /// <param name="cancellationToken">Token to cancel the run.</param>
    /// <returns>One result per strategy, in the order given.</returns>
    public async Task<IReadOnlyList<StrategyResult>> RunAsync(
        EvaluationDataset dataset,
        IReadOnlyDictionary<string, IChunkingStrategy> strategies,
        int k,
        CancellationToken cancellationToken = default)
    {
        var parser = new MarkdownDocumentParser();
        var documents = new List<Document>();
        foreach (var fixture in dataset.Documents)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fixture.Markdown));
            documents.Add(await parser.ParseAsync(new DocumentSource(fixture.Name, stream), cancellationToken).ConfigureAwait(false));
        }

        var results = new List<StrategyResult>();
        foreach (var (name, strategy) in strategies)
        {
            var chunks = documents.SelectMany(strategy.Chunk).ToList();
            var index = new InMemoryRetrievalService();
            foreach (var chunk in chunks)
            {
                var embedding = await _embeddingService.EmbedAsync(chunk.Text, cancellationToken).ConfigureAwait(false);
                await index.IndexAsync(chunk, embedding, cancellationToken).ConfigureAwait(false);
            }

            var recall = new List<double>();
            var reciprocalRank = new List<double>();
            var ndcg = new List<double>();
            foreach (var query in dataset.Queries)
            {
                bool IsRelevant(Chunk chunk) => query.KeyPhrases.Any(phrase => chunk.Text.Contains(phrase, StringComparison.OrdinalIgnoreCase));

                var queryEmbedding = await _embeddingService.EmbedAsync(query.Text, cancellationToken).ConfigureAwait(false);
                var top = await index.SearchAsync(queryEmbedding, k, cancellationToken).ConfigureAwait(false);
                var ranked = top.Select(IsRelevant).ToList();
                var totalRelevant = chunks.Count(IsRelevant);

                recall.Add(RetrievalMetrics.RecallAtK(ranked, totalRelevant, k));
                reciprocalRank.Add(RetrievalMetrics.ReciprocalRank(ranked));
                ndcg.Add(RetrievalMetrics.NdcgAtK(ranked, totalRelevant, k));
            }

            results.Add(new StrategyResult(name, chunks.Count, k, Mean(recall), Mean(reciprocalRank), Mean(ndcg)));
        }

        return results;
    }

    private static double Mean(List<double> values) => values.Count == 0 ? 0 : values.Average();
}
