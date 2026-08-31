using System.ComponentModel;
using ContextSmith.Application;
using ModelContextProtocol.Server;

namespace ContextSmith.Mcp;

/// <summary>MCP tool that parses a source document and stores it for later stages.</summary>
[McpServerToolType]
public sealed class PrepareDocumentTool
{
    /// <summary>Decodes <paramref name="contentBase64"/>, parses it, stores the document, and returns a structure summary.</summary>
    /// <param name="parserSelector">Chooses a parser from <paramref name="fileName"/>.</param>
    /// <param name="documentStore">Store the parsed document is written to.</param>
    /// <param name="fileName">File name whose extension selects the parser (for example <c>handbook.docx</c>).</param>
    /// <param name="contentBase64">Document content, base64-encoded.</param>
    /// <param name="documentId">Id to store the document under. A new id is generated when omitted.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A summary of the stored document, including the id it is stored under.</returns>
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
