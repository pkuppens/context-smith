using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.PowerPoint.Tests;

public class PowerPointDocumentParserTests
{
    private static async Task<Document> ParseAsync(params SlideSpec[] slides)
    {
        await using var deck = DeckBuilder.Build(slides);
        return await new PowerPointDocumentParser().ParseAsync(new DocumentSource("onboarding.pptx", deck));
    }

    [Fact]
    public async Task ParseAsync_maps_a_slide_to_a_section_with_its_title_and_text_blocks()
    {
        var document = await ParseAsync(new SlideSpec("Leave Policy", Texts: ["Twelve months of service.", "Apply through HR."]));

        var section = Assert.IsType<Section>(Assert.Single(document.Children));
        Assert.Equal("Leave Policy", section.Title);
        Assert.Equal("Leave Policy", Assert.IsType<Heading>(section.Children[0]).Text);
        Assert.Equal(["Twelve months of service.", "Apply through HR."], section.Children.OfType<Paragraph>().Select(p => p.Text));
        Assert.Equal("slide-1", section.Provenance.Location);
    }

    [Fact]
    public async Task ParseAsync_maps_a_table_to_a_TableBlock()
    {
        var document = await ParseAsync(new SlideSpec("Benefits", Table: [["Plan", "Cost"], ["Dental", "10"]]));

        var table = Assert.Single(document.Children.OfType<Section>().Single().Children.OfType<TableBlock>());
        Assert.Equal(["Plan", "Cost"], table.Rows[0]);
        Assert.Equal(["Dental", "10"], table.Rows[1]);
    }

    [Fact]
    public async Task ParseAsync_adds_speaker_notes_as_a_paragraph_in_the_slide_section()
    {
        var document = await ParseAsync(new SlideSpec("Leave Policy", Notes: "Mention the twelve-month rule."));

        var notes = Assert.Single(document.Children.OfType<Section>().Single().Children.OfType<Paragraph>());
        Assert.Equal("Mention the twelve-month rule.", notes.Text);
        Assert.Equal("slide-1/notes", notes.Provenance.Location);
    }

    [Fact]
    public async Task ParseAsync_names_an_untitled_slide_by_its_number_and_keeps_slide_order()
    {
        var document = await ParseAsync(new SlideSpec("First"), new SlideSpec(Texts: ["No title here."]));

        var sections = document.Children.OfType<Section>().ToList();
        Assert.Equal(["First", "Slide 2"], sections.Select(s => s.Title));
        Assert.Equal("Slide 2", Assert.IsType<Heading>(sections[1].Children[0]).Text);
    }

    [Fact]
    public async Task ParseAsync_reads_the_committed_sample_file()
    {
        await using var stream = File.OpenRead("sample.pptx");

        var document = await new PowerPointDocumentParser().ParseAsync(new DocumentSource("sample.pptx", stream));

        Assert.Equal(["Employee Onboarding", "Benefits"], document.Children.OfType<Section>().Select(s => s.Title));
    }
}
