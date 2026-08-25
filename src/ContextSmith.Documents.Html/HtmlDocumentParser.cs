using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using ContextSmith.Application;
using ContextSmith.Domain;
using Document = ContextSmith.Domain.Document;

namespace ContextSmith.Documents.Html;

public sealed class HtmlDocumentParser : IDocumentParser
{
    private static readonly HashSet<string> TransparentContainers = new(StringComparer.OrdinalIgnoreCase)
    {
        "html", "body", "div", "section", "article", "main", "header", "footer", "nav", "span",
    };

    public async Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(source.Content);
        var html = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);

        var htmlParser = new HtmlParser();
        using var dom = htmlParser.ParseDocument(html);

        var root = new PendingSection(0, null, new Provenance(source.SourceId, "body"));
        var stack = new List<PendingSection> { root };
        List<DocumentNode>? listItems = null;
        var listOrdered = false;
        var nodeIndex = 0;

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
                Provenance = new Provenance(source.SourceId, $"node-{nodeIndex}"),
                Children = listItems,
            });
            listItems = null;
        }

        void MaterializeTopSection()
        {
            var pending = stack[^1];
            stack.RemoveAt(stack.Count - 1);
            stack[^1].Children.Add(new Section
            {
                Title = pending.Title,
                Provenance = pending.Provenance,
                Children = pending.Children,
            });
        }

        void Visit(IElement element)
        {
            foreach (var child in element.Children)
            {
                nodeIndex++;
                var tag = child.TagName.ToLowerInvariant();
                var provenance = new Provenance(source.SourceId, $"node-{nodeIndex}");

                if (tag.Length == 2 && tag[0] == 'h' && tag[1] is >= '1' and <= '6')
                {
                    FlushList();
                    var level = tag[1] - '0';
                    var text = child.TextContent.Trim();

                    while (stack.Count > 1 && stack[^1].Level >= level)
                    {
                        MaterializeTopSection();
                    }

                    var pending = new PendingSection(level, text, provenance);
                    pending.Children.Add(new Heading { Text = text, Level = level, Provenance = provenance });
                    stack.Add(pending);
                    continue;
                }

                switch (tag)
                {
                    case "p":
                        FlushList();
                        var text = child.TextContent.Trim();
                        if (text.Length > 0)
                        {
                            stack[^1].Children.Add(new Paragraph { Text = text, Provenance = provenance });
                        }

                        break;

                    case "ul":
                    case "ol":
                        FlushList();
                        listOrdered = tag == "ol";
                        listItems = child.Children
                            .Where(item => item.TagName.Equals("li", StringComparison.OrdinalIgnoreCase))
                            .Select((item, i) => (DocumentNode)new Paragraph
                            {
                                Text = item.TextContent.Trim(),
                                Provenance = new Provenance(source.SourceId, $"node-{nodeIndex}-item-{i}"),
                            })
                            .ToList();
                        FlushList();
                        break;

                    case "table":
                        FlushList();
                        var rows = child.QuerySelectorAll("tr")
                            .Select(row => (IReadOnlyList<string>)row.Children
                                .Where(cell => cell.TagName is "TD" or "TH")
                                .Select(cell => cell.TextContent.Trim())
                                .ToList())
                            .Where(row => row.Count > 0)
                            .ToList();
                        if (rows.Count > 0)
                        {
                            stack[^1].Children.Add(new TableBlock { Rows = rows, Provenance = provenance });
                        }

                        break;

                    default:
                        if (TransparentContainers.Contains(tag))
                        {
                            Visit(child);
                        }

                        break;
                }
            }
        }

        Visit(dom.Body ?? (IElement)dom.DocumentElement);
        FlushList();

        while (stack.Count > 1)
        {
            MaterializeTopSection();
        }

        return new Document
        {
            Metadata = new DocumentMetadata { Title = dom.Title },
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
