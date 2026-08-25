using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Html.Tests;

public class HtmlDocumentParserTests
{
    [Fact]
    public async Task ParseAsync_maps_headings_lists_and_tables_to_the_canonical_model()
    {
        var parser = new HtmlDocumentParser();
        await using var stream = File.OpenRead("sample.html");
        var source = new DocumentSource("sample.html", stream);

        var document = await parser.ParseAsync(source);

        Assert.Equal("Employee Handbook", document.Metadata.Title);
        Assert.Equal(4, CountHeadings(document));

        var topSection = Assert.IsType<Section>(Assert.Single(document.Children));
        var nestedSections = topSection.Children.OfType<Section>().ToList();
        Assert.Equal(2, nestedSections.Count);

        var parentalLeave = Assert.IsType<Section>(Assert.Single(nestedSections[0].Children.OfType<Section>()));
        var list = Assert.IsType<ListBlock>(Assert.Single(parentalLeave.Children.OfType<ListBlock>()));
        Assert.False(list.Ordered);
        Assert.Equal(2, list.Children.Count);

        var benefits = nestedSections[1];
        var table = Assert.IsType<TableBlock>(Assert.Single(benefits.Children.OfType<TableBlock>()));
        Assert.Equal(3, table.Rows.Count);
        Assert.Equal(["Benefit", "Eligibility"], table.Rows[0]);
    }

    private static int CountHeadings(DocumentNode node)
        => node.Children.Sum(child => (child is Heading ? 1 : 0) + CountHeadings(child));
}
