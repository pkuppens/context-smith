using System.Collections.Concurrent;
using ContextSmith.Application;
using ContextSmith.Retrieval.Local;

namespace ContextSmith.Api;

public sealed class DocumentRetrievalRegistry
{
    private readonly ConcurrentDictionary<string, IRetrievalService> _byDocumentId = new();

    public IRetrievalService GetOrCreate(string documentId)
        => _byDocumentId.GetOrAdd(documentId, _ => new InMemoryRetrievalService());

    public bool TryGet(string documentId, out IRetrievalService retrievalService)
        => _byDocumentId.TryGetValue(documentId, out retrievalService!);
}
