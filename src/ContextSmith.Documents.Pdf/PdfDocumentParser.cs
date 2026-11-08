using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Pdf;

/// <summary>Parses <c>.pdf</c> files into the common <see cref="Document"/> tree through Azure Document Intelligence.</summary>
/// <param name="gateway">Gateway that returns the PDF layout.</param>
public sealed class PdfDocumentParser(IDocumentIntelligenceGateway gateway) : IDocumentParser
{
    // Page furniture repeats on every page and adds noise to retrieval, so the parser drops it.
    private static readonly HashSet<string> IgnoredRoles = ["pageHeader", "pageFooter", "pageNumber", "footnote"];

    /// <inheritdoc/>
    public async Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        var layout = await gateway.AnalyzeLayoutAsync(source.Content, cancellationToken).ConfigureAwait(false);

        var root = new PendingSection(0, null, new Provenance(source.SourceId));
        var stack = new List<PendingSection> { root };

        void CloseTop()
        {
            var pending = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            stack[^1].Children.Add(new Section { Title = pending.Title, Provenance = pending.Provenance, Children = pending.Children });
        }

        foreach (var element in layout.Elements)
        {
            var provenance = new Provenance(source.SourceId, $"page-{element.PageNumber}");
            switch (element)
            {
                case PdfParagraph { Role: not null } furniture when IgnoredRoles.Contains(furniture.Role):
                    break;
                case PdfParagraph { Role: "title" or "sectionHeading" } heading:
                    var level = heading.Role == "title" ? 1 : 2;
                    while (stack.Count > 1 && stack[^1].Level >= level)
                    {
                        CloseTop();
                    }

                    var pending = new PendingSection(level, heading.Text, provenance);
                    pending.Children.Add(new Heading { Text = heading.Text, Level = level, Provenance = provenance });
                    stack.Add(pending);
                    break;
                case PdfParagraph paragraph:
                    stack[^1].Children.Add(new Paragraph { Text = paragraph.Text, Provenance = provenance });
                    break;
                case PdfTable table:
                    stack[^1].Children.Add(new TableBlock { Rows = table.Rows, Provenance = provenance });
                    break;
            }
        }

        while (stack.Count > 1)
        {
            CloseTop();
        }

        return new Document
        {
            Metadata = new DocumentMetadata(),
            Provenance = new Provenance(source.SourceId),
            Children = root.Children,
        };
    }

    private sealed class PendingSection(int level, string? title, Provenance provenance)
    {
        public int Level { get; } = level;

        public string? Title { get; } = title;

        public Provenance Provenance { get; } = provenance;

        public List<DocumentNode> Children { get; } = [];
    }
}
