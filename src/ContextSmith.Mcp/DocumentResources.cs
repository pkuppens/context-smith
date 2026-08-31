using System.ComponentModel;
using System.Text.Json;
using ContextSmith.Application;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

/// <summary>Exposes a stored document's structure summary as an MCP resource.</summary>
[McpServerResourceType]
public sealed class DocumentResources
{
    /// <summary>Returns the JSON structure summary for the document stored under <paramref name="documentId"/>.</summary>
    /// <param name="documentId">The id returned by <c>prepare_document</c>.</param>
    /// <param name="documentStore">Store the document is read from.</param>
    /// <returns>The structure summary as a JSON text resource.</returns>
    /// <exception cref="McpException">No document is stored under <paramref name="documentId"/>.</exception>
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
