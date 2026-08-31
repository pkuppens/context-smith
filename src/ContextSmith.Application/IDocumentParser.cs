using ContextSmith.Domain;

namespace ContextSmith.Application;

/// <summary>Parses one document format into the common <see cref="Document"/> tree.</summary>
public interface IDocumentParser
{
    /// <summary>Reads <paramref name="source"/> and builds a document tree from it.</summary>
    /// <param name="source">Source identifier and content stream.</param>
    /// <param name="cancellationToken">Token to cancel the parse.</param>
    /// <returns>The parsed document.</returns>
    Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default);
}
