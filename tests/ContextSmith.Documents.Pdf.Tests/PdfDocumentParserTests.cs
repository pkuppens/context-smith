using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Pdf.Tests;

public class PdfDocumentParserTests
{
    private static async Task<Document> ParseAsync(params PdfElement[] elements)
    {
        var parser = new PdfDocumentParser(new FakeDocumentIntelligenceGateway(new PdfLayout(elements)));
        await using var stream = new MemoryStream([1, 2, 3]);
        return await parser.ParseAsync(new DocumentSource("handbook.pdf", stream));
    }

    [Fact]
    public async Task ParseAsync_turns_a_section_heading_into_a_section_with_its_paragraphs()
    {
        var document = await ParseAsync(
            new PdfParagraph("Leave Policy", "sectionHeading", 1),
            new PdfParagraph("Employees qualify after twelve months.", null, 1));

        var section = Assert.IsType<Section>(Assert.Single(document.Children));
        Assert.Equal("Leave Policy", section.Title);
        var heading = Assert.IsType<Heading>(section.Children[0]);
        Assert.Equal("Leave Policy", heading.Text);
        var paragraph = Assert.IsType<Paragraph>(section.Children[1]);
        Assert.Equal("Employees qualify after twelve months.", paragraph.Text);
        Assert.Equal(new Provenance("handbook.pdf", "page-1"), paragraph.Provenance);
    }

    [Fact]
    public async Task ParseAsync_maps_a_table_to_a_TableBlock_inside_the_current_section()
    {
        var document = await ParseAsync(
            new PdfParagraph("Benefits", "sectionHeading", 2),
            new PdfTable([["Plan", "Cost"], ["Dental", "10"]], 2));

        var section = Assert.IsType<Section>(Assert.Single(document.Children));
        var table = Assert.IsType<TableBlock>(section.Children[1]);
        Assert.Equal(["Plan", "Cost"], table.Rows[0]);
        Assert.Equal(["Dental", "10"], table.Rows[1]);
        Assert.Equal("page-2", table.Provenance.Location);
    }

    [Theory]
    [InlineData("pageHeader")]
    [InlineData("pageFooter")]
    [InlineData("pageNumber")]
    [InlineData("footnote")]
    public async Task ParseAsync_drops_page_furniture(string role)
    {
        var document = await ParseAsync(
            new PdfParagraph("Body text.", null, 1),
            new PdfParagraph("Repeated furniture", role, 1));

        var paragraph = Assert.IsType<Paragraph>(Assert.Single(document.Children));
        Assert.Equal("Body text.", paragraph.Text);
    }

    [Fact]
    public async Task ParseAsync_nests_section_headings_under_the_title()
    {
        var document = await ParseAsync(
            new PdfParagraph("Employee Handbook", "title", 1),
            new PdfParagraph("Leave Policy", "sectionHeading", 1),
            new PdfParagraph("Benefits", "sectionHeading", 2));

        var top = Assert.IsType<Section>(Assert.Single(document.Children));
        Assert.Equal("Employee Handbook", top.Title);
        Assert.Equal(["Leave Policy", "Benefits"], top.Children.OfType<Section>().Select(s => s.Title));
    }
}
