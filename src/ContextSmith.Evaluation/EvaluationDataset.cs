using System.Text.Json;

namespace ContextSmith.Evaluation;

/// <summary>A fixture document to index, as Markdown text.</summary>
/// <param name="Name">File name, used as the source id.</param>
/// <param name="Markdown">Document content.</param>
public sealed record EvaluationDocument(string Name, string Markdown);

/// <summary>
/// A query with its relevance judgment. A chunk is relevant to the query when its text contains
/// any one of <paramref name="KeyPhrases"/> (case-insensitive). Phrases, not chunk ids, carry the
/// judgment, because chunk boundaries change with every chunking strategy.
/// </summary>
/// <param name="Id">Short query identifier.</param>
/// <param name="Text">Query text.</param>
/// <param name="KeyPhrases">Phrases that mark a chunk as relevant.</param>
public sealed record EvaluationQuery(string Id, string Text, IReadOnlyList<string> KeyPhrases);

/// <summary>Fixture documents plus judged queries.</summary>
/// <param name="Documents">Documents to chunk, embed, and index.</param>
/// <param name="Queries">Judged queries.</param>
public sealed record EvaluationDataset(IReadOnlyList<EvaluationDocument> Documents, IReadOnlyList<EvaluationQuery> Queries)
{
    /// <summary>Loads every <c>*.md</c> file and <c>queries.json</c> from <paramref name="directory"/>.</summary>
    /// <param name="directory">Dataset directory.</param>
    /// <returns>The dataset.</returns>
    public static EvaluationDataset Load(string directory)
    {
        var documents = Directory.GetFiles(directory, "*.md")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(path => new EvaluationDocument(Path.GetFileName(path), File.ReadAllText(path)))
            .ToList();
        var queries = JsonSerializer.Deserialize<List<EvaluationQuery>>(
            File.ReadAllText(Path.Combine(directory, "queries.json")),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("queries.json is empty.");
        return new EvaluationDataset(documents, queries);
    }
}
