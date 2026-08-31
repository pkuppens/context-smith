using System.Collections.Concurrent;
using ContextSmith.Domain;

namespace ContextSmith.Application;

/// <summary>Process-lifetime <see cref="IDocumentStore"/> backed by a concurrent dictionary. Not persisted.</summary>
public sealed class InMemoryDocumentStore : IDocumentStore
{
    private readonly ConcurrentDictionary<string, Document> _documents = new();

    /// <inheritdoc/>
    public string Store(Document document, string? documentId = null)
    {
        var id = documentId ?? Guid.NewGuid().ToString("n");
        _documents[id] = document;
        return id;
    }

    /// <inheritdoc/>
    public Document? Get(string documentId)
        => _documents.GetValueOrDefault(documentId);
}
