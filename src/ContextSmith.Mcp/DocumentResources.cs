using System.ComponentModel;
using System.Text.Json;
using ContextSmith.Application;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

[McpServerResourceType]
public sealed class DocumentResources
{
    [McpServerResource(UriTemplate = "contextsmith://documents/{documentId}/structure", Name = "Document structure")]
    [Description("Return the canonical hierarchy for one document.")]
    public static ResourceContents GetStructure(
        [Description("The id returned by prepare_document.")] string documentId,
        IDocumentStore documentStore)
    {
        var document = documentStore.Get(documentId)
            ?? throw new McpException($"No document is stored under id '{documentId}'.");

        var summary = DocumentStructureSummaryBuilder.Build(documentId, document);
        var uri = $"contextsmith://documents/{documentId}/structure";

        return new TextResourceContents
        {
            Uri = uri,
            MimeType = "application/json",
            Text = JsonSerializer.Serialize(summary),
        };
    }
}
