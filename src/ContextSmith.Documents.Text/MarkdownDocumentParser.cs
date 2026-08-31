using System.Text.RegularExpressions;
using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Text;

/// <summary>Parses CommonMark-style Markdown into the common <see cref="Document"/> tree, handling ATX headings, list items, and paragraphs.</summary>
public sealed partial class MarkdownDocumentParser : IDocumentParser
{
    /// <inheritdoc/>
    public async Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(source.Content);
        var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        var lines = content.Replace("\r\n", "\n").Split('\n');

        var root = new PendingSection(0, null, new Provenance(source.SourceId, "body"));
        var stack = new List<PendingSection> { root };

        var paragraphLines = new List<string>();
        List<DocumentNode>? listItems = null;
        bool listOrdered = false;
        var lineNumber = 0;

        void FlushParagraph()
        {
            if (paragraphLines.Count == 0)
            {
                return;
            }

            var text = string.Join(" ", paragraphLines).Trim();
            paragraphLines.Clear();
            if (text.Length == 0)
            {
                return;
            }

            stack[^1].Children.Add(new Paragraph
            {
                Text = text,
                Provenance = new Provenance(source.SourceId, $"line-{lineNumber}"),
            });
        }

        void FlushList()
        {
            if (listItems is null || listItems.Count == 0)
            {
                listItems = null;
                return;
            }

            stack[^1].Children.Add(new ListBlock
            {
                Ordered = listOrdered,
                Provenance = new Provenance(source.SourceId, $"line-{lineNumber}"),
                Children = listItems,
            });
            listItems = null;
        }

        Section MaterializeTopSection()
        {
            var pending = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            var section = new Section
            {
                Title = pending.Title,
                Provenance = pending.Provenance,
                Children = pending.Children,
            };
            stack[^1].Children.Add(section);
            return section;
        }

        foreach (var rawLine in lines)
        {
            lineNumber++;

            var headingMatch = HeadingPattern().Match(rawLine);
            if (headingMatch.Success)
            {
                FlushParagraph();
                FlushList();

                var level = headingMatch.Groups[1].Value.Length;
                var headingText = headingMatch.Groups[2].Value.Trim();

                while (stack.Count > 1 && stack[^1].Level >= level)
                {
                    MaterializeTopSection();
                }

                var pending = new PendingSection(level, headingText, new Provenance(source.SourceId, $"line-{lineNumber}"));
                pending.Children.Add(new Heading
                {
                    Text = headingText,
                    Level = level,
                    Provenance = new Provenance(source.SourceId, $"line-{lineNumber}"),
                });
                stack.Add(pending);
                continue;
            }

            var unorderedMatch = UnorderedListItemPattern().Match(rawLine);
            var orderedMatch = OrderedListItemPattern().Match(rawLine);
            if (unorderedMatch.Success || orderedMatch.Success)
            {
                FlushParagraph();
                var ordered = orderedMatch.Success;
                var itemText = (ordered ? orderedMatch : unorderedMatch).Groups[1].Value.Trim();

                if (listItems is not null && listOrdered != ordered)
                {
                    FlushList();
                }

                listItems ??= [];
                listOrdered = ordered;
                listItems.Add(new Paragraph
                {
                    Text = itemText,
                    Provenance = new Provenance(source.SourceId, $"line-{lineNumber}"),
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(rawLine))
            {
                FlushParagraph();
                FlushList();
                continue;
            }

            paragraphLines.Add(rawLine.Trim());
        }

        FlushParagraph();
        FlushList();

        while (stack.Count > 1)
        {
            MaterializeTopSection();
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

    [GeneratedRegex(@"^(#{1,6})\s+(.*)$")]
    private static partial Regex HeadingPattern();

    [GeneratedRegex(@"^[-*+]\s+(.*)$")]
    private static partial Regex UnorderedListItemPattern();

    [GeneratedRegex(@"^\d+\.\s+(.*)$")]
    private static partial Regex OrderedListItemPattern();
}
