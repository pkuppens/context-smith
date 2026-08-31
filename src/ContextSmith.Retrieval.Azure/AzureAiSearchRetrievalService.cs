using System.Text.RegularExpressions;
using Azure.Search.Documents.Models;
using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Retrieval.Azure;

/// <summary>
/// An <see cref="IRetrievalService"/> backed by an Azure AI Search index. Each chunk is
/// stored as one search document that carries both the chunk's text/metadata and its vector
/// embedding, so a vector query against the index returns the chunk data directly without a
/// separate lookup step.
/// </summary>
public sealed partial class AzureAiSearchRetrievalService : IRetrievalService
{
    private readonly IAzureSearchGateway _gateway;
    private readonly int _vectorDimension;

    /// <summary>
    /// Creates a service that connects to Azure AI Search directly, deriving the index name
    /// from <paramref name="indexNamePrefix"/> and <paramref name="documentId"/>.
    /// </summary>
    /// <param name="endpoint">The Azure AI Search service endpoint.</param>
    /// <param name="indexNamePrefix">The prefix used to build the per-document index name.</param>
    /// <param name="documentId">The id of the document whose chunks this service indexes and searches.</param>
    /// <param name="vectorDimension">The vector dimension the index is created with, matching the configured embedding model.</param>
    /// <param name="apiKey">The Azure AI Search API key, or <see langword="null"/> to authenticate with a managed identity.</param>
    public AzureAiSearchRetrievalService(Uri endpoint, string indexNamePrefix, string documentId, int vectorDimension, string? apiKey = null)
        : this(new AzureSearchGateway(endpoint, BuildIndexName(indexNamePrefix, documentId), apiKey), vectorDimension)
    {
    }

    /// <summary>
    /// Creates a service backed by the given <paramref name="gateway"/>, used to inject a fake
    /// gateway in tests.
    /// </summary>
    /// <param name="gateway">The gateway used to talk to Azure AI Search.</param>
    /// <param name="vectorDimension">The vector dimension the index is created with, matching the configured embedding model.</param>
    public AzureAiSearchRetrievalService(IAzureSearchGateway gateway, int vectorDimension)
    {
        _gateway = gateway;
        _vectorDimension = vectorDimension;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
    /// <summary>
    /// Derives an Azure AI Search document key from <paramref name="chunkId"/> by replacing
    /// every character the service disallows in keys with an underscore. The original id is
    /// preserved verbatim in the "chunkId" field, so this never loses information.
    /// </summary>
    /// <param name="chunkId">The <see cref="Chunk.Id"/> to derive a document key from.</param>
    public static string SanitizeKey(string chunkId) => DisallowedKeyCharacters().Replace(chunkId, "_");

    /// <summary>
    /// Builds the per-document Azure AI Search index name from <paramref name="indexNamePrefix"/>
    /// and <paramref name="documentId"/>, lowercased and with every disallowed character
    /// replaced by a dash.
    /// </summary>
    /// <param name="indexNamePrefix">The prefix shared by all indexes this service creates.</param>
    /// <param name="documentId">The id of the document the index stores chunks for.</param>
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
