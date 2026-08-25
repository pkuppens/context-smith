using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Text.Tests;

public class MarkdownDocumentParserTests
{
    [Fact]
    public async Task ParseAsync_builds_nested_sections_from_heading_levels()
    {
        var parser = new MarkdownDocumentParser();
        await using var stream = File.OpenRead("sample.md");
        var source = new DocumentSource("sample.md", stream);

        var document = await parser.ParseAsync(source);

        Assert.Equal(4, CountHeadings(document));
        Assert.Equal(3, MaxSectionDepth(document));

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
    }

    private static int CountHeadings(DocumentNode node)
        => node.Children.Sum(child => (child is Heading ? 1 : 0) + CountHeadings(child));

    private static int MaxSectionDepth(DocumentNode node)
    {
        var childSections = node.Children.OfType<Section>().ToList();
        if (childSections.Count == 0)
        {
            return node is Section ? 1 : 0;
        }

        var deepest = childSections.Max(MaxSectionDepth);
        return node is Section ? deepest + 1 : deepest;
    }
}
