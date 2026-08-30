using Azure.Search.Documents.Models;

namespace ContextSmith.Retrieval.Azure;

// A narrow seam over the Azure AI Search SDK, so AzureAiSearchRetrievalService's chunk
// mapping and behavior can be unit-tested against a fake gateway instead of a real
// Azure AI Search resource.
public interface IAzureSearchGateway
{
    Task EnsureIndexAsync(int vectorDimension, CancellationToken cancellationToken);

    Task MergeOrUploadAsync(SearchDocument document, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchDocument>> VectorSearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken);
}
