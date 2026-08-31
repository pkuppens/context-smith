using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace ContextSmith.Retrieval.Azure;

/// <summary><see cref="IAzureSearchGateway"/> that talks to a real Azure AI Search service through the Azure SDK.</summary>
public sealed class AzureSearchGateway : IAzureSearchGateway
{
    /// <summary>Name of the vector field in the search index.</summary>
    public const string VectorFieldName = "vector";
    private const string VectorSearchProfileName = "contextsmith-hnsw-profile";
    private const string VectorSearchAlgorithmName = "contextsmith-hnsw";

    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly string _indexName;
    private bool _indexEnsured;

    /// <summary>Creates a gateway for one Azure AI Search index.</summary>
    /// <param name="endpoint">Azure AI Search service endpoint.</param>
    /// <param name="indexName">Name of the index this gateway reads and writes.</param>
    /// <param name="apiKey">Admin API key, or <see langword="null"/> to authenticate with a managed identity.</param>
    public AzureSearchGateway(Uri endpoint, string indexName, string? apiKey = null)
    {
        _indexName = indexName;

        if (apiKey is null)
        {
            TokenCredential tokenCredential = new DefaultAzureCredential();
            _indexClient = new SearchIndexClient(endpoint, tokenCredential);
            _searchClient = new SearchClient(endpoint, indexName, tokenCredential);
        }
        else
        {
            var keyCredential = new AzureKeyCredential(apiKey);
            _indexClient = new SearchIndexClient(endpoint, keyCredential);
            _searchClient = new SearchClient(endpoint, indexName, keyCredential);
        }
    }

    /// <inheritdoc/>
    public async Task EnsureIndexAsync(int vectorDimension, CancellationToken cancellationToken)
    {
        if (_indexEnsured)
        {
            return;
        }

        var index = new SearchIndex(_indexName)
        {
            Fields =
            {
                new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                new SimpleField("chunkId", SearchFieldDataType.String),
                new SearchableField("text"),
                new SimpleField("sourceId", SearchFieldDataType.String) { IsFilterable = true },
                new SimpleField("location", SearchFieldDataType.String),
                new SimpleField("headingPath", SearchFieldDataType.Collection(SearchFieldDataType.String)),
                new SearchField(VectorFieldName, SearchFieldDataType.Collection(SearchFieldDataType.Single))
                {
                    VectorSearchDimensions = vectorDimension,
                    VectorSearchProfileName = VectorSearchProfileName,
                },
            },
            VectorSearch = new()
            {
                Profiles = { new VectorSearchProfile(VectorSearchProfileName, VectorSearchAlgorithmName) },
                Algorithms = { new HnswAlgorithmConfiguration(VectorSearchAlgorithmName) },
            },
        };

        await _indexClient.CreateOrUpdateIndexAsync(index, cancellationToken: cancellationToken).ConfigureAwait(false);
        _indexEnsured = true;
    }

    /// <inheritdoc/>
    public async Task MergeOrUploadAsync(SearchDocument document, CancellationToken cancellationToken) =>
        await _searchClient.MergeOrUploadDocumentsAsync([document], cancellationToken: cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SearchDocument>> VectorSearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken)
    {
        var options = new SearchOptions
        {
            VectorSearch = new()
            {
                Queries = { new VectorizedQuery(queryEmbedding) { KNearestNeighborsCount = topK, Fields = { VectorFieldName } } },
            },
        };

        var response = await _searchClient.SearchAsync<SearchDocument>(searchText: null, options, cancellationToken).ConfigureAwait(false);

        var results = new List<SearchDocument>();
        await foreach (var result in response.Value.GetResultsAsync().WithCancellation(cancellationToken))
        {
            results.Add(result.Document);
        }

        return results;
    }
}
