using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;

namespace ContextSmith.Retrieval.Azure;

public sealed class AzureSearchGateway : IAzureSearchGateway
{
    public const string VectorFieldName = "vector";
    private const string VectorSearchProfileName = "contextsmith-hnsw-profile";
    private const string VectorSearchAlgorithmName = "contextsmith-hnsw";

    private readonly SearchIndexClient _indexClient;
    private readonly SearchClient _searchClient;
    private readonly string _indexName;
    private bool _indexEnsured;

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

    public async Task MergeOrUploadAsync(SearchDocument document, CancellationToken cancellationToken) =>
        await _searchClient.MergeOrUploadDocumentsAsync([document], cancellationToken: cancellationToken).ConfigureAwait(false);

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
