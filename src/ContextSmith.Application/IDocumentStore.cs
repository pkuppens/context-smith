using ContextSmith.Domain;

namespace ContextSmith.Application;

/// <summary>Stores parsed documents and retrieves them by identifier.</summary>
public interface IDocumentStore
{
    /// <summary>Stores <paramref name="document"/> and returns the identifier it is stored under.</summary>
    /// <param name="document">Document to store.</param>
    /// <param name="documentId">Identifier to use. When <see langword="null"/>, the store generates one.</param>
    /// <returns>The identifier the document is stored under.</returns>
    string Store(Document document, string? documentId = null);

    /// <summary>Returns the document stored under <paramref name="documentId"/>, or <see langword="null"/> when none is stored.</summary>
    /// <param name="documentId">Identifier to look up.</param>
    Document? Get(string documentId);
}
