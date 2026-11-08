using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using Azure.Identity;

namespace ContextSmith.Documents.Pdf;

/// <summary>Calls the Azure Document Intelligence <c>prebuilt-layout</c> model.</summary>
public sealed class AzureDocumentIntelligenceGateway : IDocumentIntelligenceGateway
{
    private readonly DocumentIntelligenceClient _client;

    /// <summary>Creates a gateway for <paramref name="endpoint"/>.</summary>
    /// <param name="endpoint">Document Intelligence resource endpoint.</param>
    /// <param name="apiKey">API key. When <see langword="null"/> or empty, <see cref="DefaultAzureCredential"/> is used (managed identity in Azure).</param>
    public AzureDocumentIntelligenceGateway(Uri endpoint, string? apiKey = null)
    {
        _client = string.IsNullOrWhiteSpace(apiKey)
            ? new DocumentIntelligenceClient(endpoint, (TokenCredential)new DefaultAzureCredential())
            : new DocumentIntelligenceClient(endpoint, new AzureKeyCredential(apiKey));
    }

    /// <inheritdoc/>
    public async Task<PdfLayout> AnalyzeLayoutAsync(Stream content, CancellationToken cancellationToken = default)
    {
        var bytes = await BinaryData.FromStreamAsync(content, cancellationToken).ConfigureAwait(false);
        var operation = await _client
            .AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", bytes, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return DocumentIntelligenceLayoutMapper.Map(operation.Value);
    }
}
