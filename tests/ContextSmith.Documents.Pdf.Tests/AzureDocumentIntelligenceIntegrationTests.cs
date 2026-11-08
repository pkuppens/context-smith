using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Pdf.Tests;

// Runs the real parser against Azure Document Intelligence. Skipped (not failed) when
// AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT is unset, using the pattern from the Azure retrieval tests.
public class AzureDocumentIntelligenceIntegrationTests
{
    [SkippableFact]
    public async Task ParseAsync_reads_text_from_the_fixture_pdf()
    {
        var endpoint = Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT");
        Skip.If(string.IsNullOrWhiteSpace(endpoint), "Set AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT (and optionally AZURE_DOCUMENT_INTELLIGENCE_API_KEY) to run this test.");

        var parser = new PdfDocumentParser(new AzureDocumentIntelligenceGateway(
            new Uri(endpoint!),
            Environment.GetEnvironmentVariable("AZURE_DOCUMENT_INTELLIGENCE_API_KEY")));
        await using var stream = File.OpenRead("sample.pdf");

        var document = await parser.ParseAsync(new DocumentSource("sample.pdf", stream));

        Assert.Contains("parental leave", Flatten(document), StringComparison.OrdinalIgnoreCase);
    }

    private static string Flatten(DocumentNode node)
        => string.Join(' ', node.Children.Select(child => child switch
        {
            Paragraph paragraph => paragraph.Text,
            Heading heading => heading.Text,
            _ => Flatten(child),
        }));
}
