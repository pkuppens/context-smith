namespace ContextSmith.Domain.Tests;

public class DocumentTreeTests
{
    [Fact]
    public void Document_tree_preserves_structure_and_provenance()
    {
        var heading = new Heading
        {
            Text = "Eligibility",
            Level = 2,
            Provenance = new Provenance("handbook.docx", "section-1/heading-1"),
        };
        var paragraph = new Paragraph
        {
            Text = "Employees qualify after twelve months of employment.",
            Provenance = new Provenance("handbook.docx", "section-1/paragraph-1"),
        };
        var section = new Section
        {
            Title = "Eligibility",
            Provenance = new Provenance("handbook.docx", "section-1"),
            Children = [heading, paragraph],
        };
        var document = new Document
        {
            Metadata = new DocumentMetadata { Title = "Employee Handbook" },
            Provenance = new Provenance("handbook.docx"),
            Children = [section],
        };

        Assert.Equal("Employee Handbook", document.Metadata.Title);
        Assert.Single(document.Children);
        Assert.Same(section, document.Children[0]);

        Assert.Equal(2, section.Children.Count);
        Assert.Same(heading, section.Children[0]);
        Assert.Same(paragraph, section.Children[1]);

        Assert.Equal("handbook.docx", document.Provenance.SourceId);
        Assert.Equal("section-1/heading-1", heading.Provenance.Location);
        Assert.Equal("section-1/paragraph-1", paragraph.Provenance.Location);
    }
}
