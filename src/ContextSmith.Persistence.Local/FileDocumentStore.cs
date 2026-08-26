using System.Text.Json;
using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Persistence.Local;

public sealed class FileDocumentStore(string rootDirectory) : IDocumentStore
{
    private readonly string _documentsDirectory = Path.Combine(rootDirectory, "documents");

    public string Store(Document document, string? documentId = null)
    {
        var id = documentId ?? Guid.NewGuid().ToString("n");
        Directory.CreateDirectory(_documentsDirectory);
        File.WriteAllText(PathFor(id), JsonSerializer.Serialize(document));
        return id;
    }

    public Document? Get(string documentId)
    {
        var path = PathFor(documentId);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<Document>(File.ReadAllText(path))
            : null;
    }

    private string PathFor(string documentId) => Path.Combine(_documentsDirectory, $"{documentId}.json");
}
