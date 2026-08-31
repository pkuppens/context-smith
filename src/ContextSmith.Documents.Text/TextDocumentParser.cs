using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Text;

/// <summary>Parses plain text into the common <see cref="Document"/> tree, treating blank-line-separated blocks as paragraphs.</summary>
public sealed class TextDocumentParser : IDocumentParser
{
    /// <inheritdoc/>
    public async Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(source.Content);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        var blocks = content
            .Replace("\r\n", "\n")
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(block => block.Length > 0)
            .ToList();

        var paragraphs = blocks
            .Select((block, index) => (DocumentNode)new Paragraph
            {
                Text = block.Replace('\n', ' '),
                Provenance = new Provenance(source.SourceId, $"block-{index}"),
            })
            .ToList();

        var section = new Section
        {
            Provenance = new Provenance(source.SourceId, "body"),
            Children = paragraphs,
        };

        return new Document
        {
            Metadata = new DocumentMetadata(),
            Provenance = new Provenance(source.SourceId),
            Children = [section],
        };
    }
}
