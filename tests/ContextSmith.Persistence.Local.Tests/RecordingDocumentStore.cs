using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Persistence.Local.Tests;

/// <summary>A no-op <see cref="IDocumentStore"/> that records calls instead of touching real storage.</summary>
public sealed class RecordingDocumentStore : IDocumentStore
{
    public List<(Document Document, string? DocumentId)> StoreCalls { get; } = [];

    public string Store(Document document, string? documentId = null)
    {
        StoreCalls.Add((document, documentId));
        return documentId ?? "recorded-id";
    }

    public Document? Get(string documentId) => null;
}
