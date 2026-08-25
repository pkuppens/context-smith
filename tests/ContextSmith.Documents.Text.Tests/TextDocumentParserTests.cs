using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Text.Tests;

public class TextDocumentParserTests
{
    [Fact]
    public async Task ParseAsync_splits_blank_line_separated_blocks_into_paragraphs()
    {
        var parser = new TextDocumentParser();
        await using var stream = File.OpenRead("sample.txt");
        var source = new DocumentSource("sample.txt", stream);

        var document = await parser.ParseAsync(source);

        var section = Assert.Single(document.Children);
        var paragraphs = section.Children;
        Assert.Equal(3, paragraphs.Count);
        Assert.All(paragraphs, node => Assert.IsType<Paragraph>(node));
        Assert.Equal("sample.txt", document.Provenance.SourceId);
    }
}
