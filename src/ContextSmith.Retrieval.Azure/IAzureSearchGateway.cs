using Azure.Search.Documents.Models;

namespace ContextSmith.Retrieval.Azure;

/// <summary>
/// A narrow seam over the Azure AI Search SDK, so <see cref="AzureAiSearchRetrievalService"/>'s chunk
/// mapping and behaviour can be unit-tested against a fake gateway instead of a real Azure AI Search resource.
/// </summary>
public interface IAzureSearchGateway
{
    /// <summary>Creates the search index if it does not exist yet. Safe to call repeatedly.</summary>
    /// <param name="vectorDimension">Dimension of the vector field, fixed at index creation.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    Task EnsureIndexAsync(int vectorDimension, CancellationToken cancellationToken);

    /// <summary>Inserts <paramref name="document"/>, or merges it into the existing document with the same key.</summary>
    /// <param name="document">Search document to upload.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    Task MergeOrUploadAsync(SearchDocument document, CancellationToken cancellationToken);

    /// <summary>Runs a vector similarity query and returns the nearest documents.</summary>
    /// <param name="queryEmbedding">Query vector.</param>
    /// <param name="topK">Maximum number of documents to return.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    /// <returns>The matching documents, ordered from nearest to farthest.</returns>
    Task<IReadOnlyList<SearchDocument>> VectorSearchAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken);
}
