using ContextSmith.Documents.Pdf;

namespace ContextSmith.Documents.Pdf.Tests;

/// <summary>Returns a canned layout so parser tests never call Azure.</summary>
internal sealed class FakeDocumentIntelligenceGateway(PdfLayout layout) : IDocumentIntelligenceGateway
{
    public Task<PdfLayout> AnalyzeLayoutAsync(Stream content, CancellationToken cancellationToken = default)
        => Task.FromResult(layout);
}
