using System.Text.RegularExpressions;
using Azure.Search.Documents.Models;
using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Retrieval.Azure;

public sealed partial class AzureAiSearchRetrievalService : IRetrievalService
{
    private readonly IAzureSearchGateway _gateway;
    private readonly int _vectorDimension;

    public AzureAiSearchRetrievalService(Uri endpoint, string indexNamePrefix, string documentId, int vectorDimension, string? apiKey = null)
        : this(new AzureSearchGateway(endpoint, BuildIndexName(indexNamePrefix, documentId), apiKey), vectorDimension)
    {
    }

    public AzureAiSearchRetrievalService(IAzureSearchGateway gateway, int vectorDimension)
    {
        _gateway = gateway;
        _vectorDimension = vectorDimension;
    }

    public async Task IndexAsync(Chunk chunk, float[] embedding, CancellationToken cancellationToken = default)
    {
        await _gateway.EnsureIndexAsync(_vectorDimension, cancellationToken).ConfigureAwait(false);

        var document = new SearchDocument
        {
            ["id"] = SanitizeKey(chunk.Id),
            ["chunkId"] = chunk.Id,
            ["text"] = chunk.Text,
            ["sourceId"] = chunk.Provenance.SourceId,
            ["location"] = chunk.Provenance.Location,
            ["headingPath"] = chunk.HeadingPath.ToArray(),
            [AzureSearchGateway.VectorFieldName] = embedding,
        };

        await _gateway.MergeOrUploadAsync(document, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Chunk>> SearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default)
    {
        await _gateway.EnsureIndexAsync(_vectorDimension, cancellationToken).ConfigureAwait(false);

        var documents = await _gateway.VectorSearchAsync(queryEmbedding, topK, cancellationToken).ConfigureAwait(false);

        return documents.Select(ToChunk).ToList();
    }

    private static Chunk ToChunk(SearchDocument document)
    {
        var chunkId = (string)document["chunkId"]!;
        var text = (string)document["text"]!;
        var sourceId = (string)document["sourceId"]!;
        var location = document.TryGetValue("location", out var locationValue) ? (string?)locationValue : null;
        var headingPath = document.TryGetValue("headingPath", out var headingPathValue) && headingPathValue is not null
            ? ((IEnumerable<object>)headingPathValue).Select(value => (string)value).ToList()
            : [];

        return new Chunk(chunkId, text, new Provenance(sourceId, location), headingPath);
    }

    // Azure AI Search document keys allow only letters, digits, underscore, dash, and equal
    // sign. A Chunk.Id may use other characters, so the key is a sanitized derivative; the
    // original id is preserved verbatim in the "chunkId" field and used to reconstruct the
    // Chunk on read, so sanitization never loses information.
    public static string SanitizeKey(string chunkId) => DisallowedKeyCharacters().Replace(chunkId, "_");

    public static string BuildIndexName(string indexNamePrefix, string documentId)
    {
        var name = $"{indexNamePrefix}-{documentId}".ToLowerInvariant();
        name = DisallowedIndexNameCharacters().Replace(name, "-");
        return name.Trim('-');
    }

    [GeneratedRegex("[^A-Za-z0-9_=-]")]
    private static partial Regex DisallowedKeyCharacters();

    [GeneratedRegex("[^a-z0-9-]")]
    private static partial Regex DisallowedIndexNameCharacters();
}
