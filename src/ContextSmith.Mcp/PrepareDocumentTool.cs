using System.ComponentModel;
using ContextSmith.Application;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

[McpServerToolType]
public sealed class PrepareDocumentTool
{
    [McpServerTool(Name = "prepare_document"), Description(
        "Convert a source document into the normalized model that later stages can use.")]
    public static async Task<DocumentStructureSummary> PrepareDocument(
        IDocumentParserSelector parserSelector,
        IDocumentStore documentStore,
        [Description("The file name, used to select a parser by extension (e.g. 'handbook.docx').")] string fileName,
        [Description("The document content, base64-encoded.")] string contentBase64,
        [Description("An id to store the document under. A new id is generated when omitted.")] string? documentId = null,
        CancellationToken cancellationToken = default)
    {
        var parser = parserSelector.GetParser(fileName);

        var bytes = Convert.FromBase64String(contentBase64);
        using var stream = new MemoryStream(bytes);
        var source = new DocumentSource(fileName, stream);

        var document = await parser.ParseAsync(source, cancellationToken).ConfigureAwait(false);
        var storedId = documentStore.Store(document, documentId);

        return DocumentStructureSummaryBuilder.Build(storedId, document);
    }
}
