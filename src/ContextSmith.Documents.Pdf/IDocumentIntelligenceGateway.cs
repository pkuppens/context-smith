namespace ContextSmith.Documents.Pdf;

/// <summary>Seam over Azure Document Intelligence, so the parser can be tested without Azure.</summary>
public interface IDocumentIntelligenceGateway
{
    /// <summary>Runs layout analysis on a PDF stream.</summary>
    /// <param name="content">PDF bytes.</param>
    /// <param name="cancellationToken">Token to cancel the call.</param>
    /// <returns>The layout in reading order.</returns>
    Task<PdfLayout> AnalyzeLayoutAsync(Stream content, CancellationToken cancellationToken = default);
}
