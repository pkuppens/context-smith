using System.Text.Json;
using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Persistence.Local;

/// <summary><see cref="IDocumentStore"/> that persists each document as a JSON file under <c>documents/</c> in a root directory.</summary>
/// <param name="rootDirectory">Directory that holds the <c>documents/</c> folder.</param>
public sealed class FileDocumentStore(string rootDirectory) : IDocumentStore
{
    private readonly string _documentsDirectory = Path.Combine(rootDirectory, "documents");

    /// <inheritdoc/>
    public string Store(Document document, string? documentId = null)
    {
        var id = documentId ?? Guid.NewGuid().ToString("n");
        Directory.CreateDirectory(_documentsDirectory);
        File.WriteAllText(PathFor(id), JsonSerializer.Serialize(document));
        return id;
    }

    /// <inheritdoc/>
    public Document? Get(string documentId)
    {
        var path = PathFor(documentId);
        return File.Exists(path)
            ? JsonSerializer.Deserialize<Document>(File.ReadAllText(path))
            : null;
    }

    private string PathFor(string documentId) => Path.Combine(_documentsDirectory, $"{documentId}.json");
}
