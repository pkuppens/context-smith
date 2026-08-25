using ContextSmith.Domain;

namespace ContextSmith.Application;

public interface IDocumentParser
{
    Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default);
}
