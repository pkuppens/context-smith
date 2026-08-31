using ContextSmith.Domain;

namespace ContextSmith.Application;

/// <summary>Builds a <see cref="DocumentStructureSummary"/> from a parsed <see cref="Document"/>.</summary>
public static class DocumentStructureSummaryBuilder
{
    /// <summary>Counts the section, heading, and paragraph nodes in <paramref name="document"/> and builds its outline tree.</summary>
    /// <param name="documentId">Identifier to record on the returned summary.</param>
    /// <param name="document">Parsed document to summarise.</param>
    /// <returns>A summary of <paramref name="document"/>.</returns>
    public static DocumentStructureSummary Build(string documentId, Document document)
    {
        var outline = BuildOutline(document, level: 0);
        return new DocumentStructureSummary(
            documentId,
            SectionCount: CountNodes<Section>(document),
            HeadingCount: CountNodes<Heading>(document),
            ParagraphCount: CountNodes<Paragraph>(document),
            Outline: outline);
    }

    private static DocumentOutlineNode BuildOutline(DocumentNode node, int level)
    {
        var title = node switch
        {
            Document document => document.Metadata.Title,
            Section section => section.Title,
            _ => null,
        };

        var children = node.Children
            .OfType<Section>()
            .Select(section => BuildOutline(section, level + 1))
            .ToList();

        return new DocumentOutlineNode(title, level, children);
    }

    private static int CountNodes<T>(DocumentNode node)
        where T : DocumentNode
        => node.Children.Sum(child => (child is T ? 1 : 0) + CountNodes<T>(child));
}
