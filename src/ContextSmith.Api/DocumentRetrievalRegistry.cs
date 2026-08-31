using System.Collections.Concurrent;
using ContextSmith.Application;
using ContextSmith.Retrieval.Local;

namespace ContextSmith.Api;

/// <summary>Holds one <see cref="IRetrievalService"/> per document id, creating it on first use.</summary>
/// <param name="createRetrievalService">
/// Factory that builds a retrieval service for a document id. When <see langword="null"/>, an
/// <see cref="InMemoryRetrievalService"/> is used.
/// </param>
public sealed class DocumentRetrievalRegistry(Func<string, IRetrievalService>? createRetrievalService = null)
{
    private readonly Func<string, IRetrievalService> _createRetrievalService = createRetrievalService ?? (_ => new InMemoryRetrievalService());
    private readonly ConcurrentDictionary<string, IRetrievalService> _byDocumentId = new();

    /// <summary>Returns the retrieval service for <paramref name="documentId"/>, creating it if needed.</summary>
    /// <param name="documentId">Document whose retrieval service is requested.</param>
    public IRetrievalService GetOrCreate(string documentId)
        => _byDocumentId.GetOrAdd(documentId, _createRetrievalService);
}
