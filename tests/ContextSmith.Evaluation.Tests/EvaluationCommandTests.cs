using System.Globalization;
using ContextSmith.Evaluation;

namespace ContextSmith.Evaluation.Tests;

public class EvaluationCommandTests
{
    [Fact]
    public async Task RunAsync_writes_a_report_that_compares_two_chunking_strategies_with_metrics_between_0_and_1()
    {
        var reportPath = Path.Combine(Path.GetTempPath(), "context-smith-tests", Guid.NewGuid().ToString("n"), "report.csv");

        var exitCode = await EvaluationCommand.RunAsync(["evaluation", reportPath, "3"], TextWriter.Null);

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(reportPath));
        var lines = File.ReadAllLines(reportPath);
        Assert.Equal("strategy,chunks,k,recall_at_k,mrr,ndcg_at_k", lines[0]);
        Assert.True(lines.Length >= 3, "Expected a header and at least two strategy rows.");
        foreach (var row in lines.Skip(1))
        {
            var cells = row.Split(',');
            foreach (var metric in cells.Skip(3))
            {
                var value = double.Parse(metric, CultureInfo.InvariantCulture);
                Assert.InRange(value, 0.0, 1.0);
            }
        }
    }

    [Fact]
    public async Task RunAsync_reports_usage_and_fails_when_the_dataset_directory_is_missing()
    {
        var output = new StringWriter();

        var exitCode = await EvaluationCommand.RunAsync(["does-not-exist", "report.csv", "3"], output);

        Assert.Equal(1, exitCode);
        Assert.Contains("does-not-exist", output.ToString());
    }
}
