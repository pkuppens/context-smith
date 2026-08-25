using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Docx.Tests;

public class DocxDocumentParserTests
{
    [Fact]
    public async Task ParseAsync_maps_styles_and_numbering_to_the_canonical_model()
    {
        var parser = new DocxDocumentParser();
        await using var stream = File.OpenRead("sample.docx");
        var source = new DocumentSource("sample.docx", stream);

        var document = await parser.ParseAsync(source);

        Assert.Equal(4, CountHeadings(document));

        var topSection = Assert.IsType<Section>(Assert.Single(document.Children));
        Assert.Equal("Employee Handbook", topSection.Title);

        var nestedSections = topSection.Children.OfType<Section>().ToList();
        Assert.Equal(2, nestedSections.Count);
        Assert.Equal("Leave Policy", nestedSections[0].Title);
        Assert.Equal("Benefits", nestedSections[1].Title);

        var parentalLeave = Assert.IsType<Section>(Assert.Single(nestedSections[0].Children.OfType<Section>()));
        Assert.Equal("Parental Leave", parentalLeave.Title);

        var list = Assert.IsType<ListBlock>(Assert.Single(parentalLeave.Children.OfType<ListBlock>()));
        Assert.False(list.Ordered);
        Assert.Equal(2, list.Children.Count);
        Assert.All(list.Children, node => Assert.IsType<Paragraph>(node));
    }

    private static int CountHeadings(DocumentNode node)
        => node.Children.Sum(child => (child is Heading ? 1 : 0) + CountHeadings(child));
}
