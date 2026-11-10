using System.Globalization;
using ContextSmith.Application;

namespace ContextSmith.Evaluation;

/// <summary>Entry point logic of the evaluation tool: load the dataset, compare chunking strategies, write a CSV report and a console table.</summary>
public static class EvaluationCommand
{
    private const string Header = "strategy,chunks,k,recall_at_k,mrr,ndcg_at_k";

    /// <summary>Runs the evaluation.</summary>
    /// <param name="args">Optional: dataset directory, report path, and k. Defaults are <c>samples/documents/evaluation</c>, <c>evaluation-report.csv</c>, and 3.</param>
    /// <param name="output">Writer for the console table and error messages.</param>
    /// <returns>Process exit code: 0 on success, 1 when the dataset directory is missing.</returns>
    public static async Task<int> RunAsync(string[] args, TextWriter output)
    {
        var datasetDirectory = args.Length > 0 ? args[0] : Path.Combine("samples", "documents", "evaluation");
        var reportPath = args.Length > 1 ? args[1] : "evaluation-report.csv";
        var k = args.Length > 2 ? int.Parse(args[2], CultureInfo.InvariantCulture) : 3;

        if (!Directory.Exists(datasetDirectory))
        {
            await output.WriteLineAsync($"Dataset directory '{datasetDirectory}' does not exist. Usage: <datasetDirectory> <reportPath> <k>").ConfigureAwait(false);
            return 1;
        }

        var dataset = EvaluationDataset.Load(datasetDirectory);
        var strategies = new Dictionary<string, IChunkingStrategy>
        {
            ["fixed-size-300"] = new FixedSizeChunker(300),
            ["structure-aware"] = new StructureAwareChunker(),
        };

        var results = await new EvaluationRunner().RunAsync(dataset, strategies, k).ConfigureAwait(false);

        var rows = results.Select(r => string.Join(
            ',',
            r.Strategy,
            r.ChunkCount.ToString(CultureInfo.InvariantCulture),
            r.K.ToString(CultureInfo.InvariantCulture),
            r.RecallAtK.ToString("0.0000", CultureInfo.InvariantCulture),
            r.MeanReciprocalRank.ToString("0.0000", CultureInfo.InvariantCulture),
            r.Ndcg.ToString("0.0000", CultureInfo.InvariantCulture))).ToList();

        var directory = Path.GetDirectoryName(Path.GetFullPath(reportPath));
        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllLinesAsync(reportPath, [Header, .. rows]).ConfigureAwait(false);

        await output.WriteLineAsync($"{"strategy",-18}{"chunks",8}{"recall@" + k,10}{"mrr",8}{"ndcg@" + k,10}").ConfigureAwait(false);
        foreach (var r in results)
        {
            await output.WriteLineAsync($"{r.Strategy,-18}{r.ChunkCount,8}{r.RecallAtK,10:0.000}{r.MeanReciprocalRank,8:0.000}{r.Ndcg,10:0.000}").ConfigureAwait(false);
        }

        await output.WriteLineAsync($"Report written to {reportPath}").ConfigureAwait(false);
        return 0;
    }
}
