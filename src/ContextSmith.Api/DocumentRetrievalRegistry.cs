using System.Collections.Concurrent;
using ContextSmith.Application;
using ContextSmith.Retrieval.Local;

namespace ContextSmith.Api;

public sealed class DocumentRetrievalRegistry(Func<string, IRetrievalService>? createRetrievalService = null)
{
    private readonly Func<string, IRetrievalService> _createRetrievalService = createRetrievalService ?? (_ => new InMemoryRetrievalService());
    private readonly ConcurrentDictionary<string, IRetrievalService> _byDocumentId = new();

    public IRetrievalService GetOrCreate(string documentId)
        => _byDocumentId.GetOrAdd(documentId, _createRetrievalService);
}
