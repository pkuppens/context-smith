using ContextSmith.Domain;

namespace ContextSmith.Application;

public interface IDocumentStore
{
    string Store(Document document, string? documentId = null);

    Document? Get(string documentId);
}
