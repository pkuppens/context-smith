using System.Collections.Concurrent;
using ContextSmith.Domain;

namespace ContextSmith.Application;

public sealed class InMemoryDocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<string, Document> _documents = new();

    public string Store(Document document, string? documentId = null)
    {
        var id = documentId ?? Guid.NewGuid().ToString("n");
        _documents[id] = document;
        return id;
    }

    public Document? Get(string documentId)
        => _documents.GetValueOrDefault(documentId);
}
