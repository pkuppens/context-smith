using ContextSmith.Application;
using ContextSmith.Domain;
using DocumentFormat.OpenXml.Packaging;
using AbstractNum = DocumentFormat.OpenXml.Wordprocessing.AbstractNum;
using Level = DocumentFormat.OpenXml.Wordprocessing.Level;
using NumberFormatValues = DocumentFormat.OpenXml.Wordprocessing.NumberFormatValues;
using NumberingInstance = DocumentFormat.OpenXml.Wordprocessing.NumberingInstance;
using OpenXmlParagraph = DocumentFormat.OpenXml.Wordprocessing.Paragraph;
using OpenXmlText = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace ContextSmith.Documents.Docx;

public sealed class DocxDocumentParser : IDocumentParser
{
    public Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        using var wordDocument = WordprocessingDocument.Open(source.Content, isEditable: false);
        var mainPart = wordDocument.MainDocumentPart
            ?? throw new InvalidOperationException($"'{source.SourceId}' has no main document part.");
        var body = mainPart.Document?.Body
            ?? throw new InvalidOperationException($"'{source.SourceId}' has no document body.");

        var root = new PendingSection(0, null, new Provenance(source.SourceId, "body"));
        var stack = new List<PendingSection> { root };
        List<DocumentNode>? listItems = null;
        var listOrdered = false;
        var paragraphIndex = 0;

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
                Provenance = new Provenance(source.SourceId, $"paragraph-{paragraphIndex}"),
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

        foreach (var paragraph in body.Elements<OpenXmlParagraph>())
        {
            paragraphIndex++;
            var text = string.Concat(paragraph.Descendants<OpenXmlText>().Select(t => t.Text)).Trim();
            var provenance = new Provenance(source.SourceId, $"paragraph-{paragraphIndex}");

            var headingLevel = GetHeadingLevel(paragraph);
            if (headingLevel is int level)
            {
                FlushList();

                while (stack.Count > 1 && stack[^1].Level >= level)
                {
                    MaterializeTopSection();
                }

                var pending = new PendingSection(level, text, provenance);
                pending.Children.Add(new Heading { Text = text, Level = level, Provenance = provenance });
                stack.Add(pending);
                continue;
            }

            if (text.Length == 0)
            {
                continue;
            }

            var numberingId = paragraph.ParagraphProperties?.NumberingProperties?.NumberingId?.Val?.Value;
            if (numberingId is int id)
            {
                var ordered = IsOrderedList(mainPart, id);
                if (listItems is not null && listOrdered != ordered)
                {
                    FlushList();
                }

                listItems ??= [];
                listOrdered = ordered;
                listItems.Add(new Paragraph { Text = text, Provenance = provenance });
                continue;
            }

            FlushList();
            stack[^1].Children.Add(new Paragraph { Text = text, Provenance = provenance });
        }

        FlushList();

        while (stack.Count > 1)
        {
            MaterializeTopSection();
        }

        var document = new Document
        {
            Metadata = new DocumentMetadata(),
            Provenance = new Provenance(source.SourceId),
            Children = root.Children,
        };

        return Task.FromResult(document);
    }

    private static int? GetHeadingLevel(OpenXmlParagraph paragraph)
    {
        var styleId = paragraph.ParagraphProperties?.ParagraphStyleId?.Val?.Value;
        if (styleId is null)
        {
            return null;
        }

        if (styleId.StartsWith("Heading", StringComparison.Ordinal)
            && int.TryParse(styleId.AsSpan("Heading".Length), out var level)
            && level is >= 1 and <= 9)
        {
            return level;
        }

        return styleId switch
        {
            "Title" => 1,
            _ => null,
        };
    }

    private static bool IsOrderedList(MainDocumentPart mainPart, int numberingId)
    {
        var numbering = mainPart.NumberingDefinitionsPart?.Numbering;
        var numberingInstance = numbering?
            .Elements<NumberingInstance>()
            .FirstOrDefault(instance => instance.NumberID?.Value == numberingId);
        var abstractNumId = numberingInstance?.AbstractNumId?.Val?.Value;

        var abstractNum = numbering?
            .Elements<AbstractNum>()
            .FirstOrDefault(candidate => candidate.AbstractNumberId?.Value == abstractNumId);
        var format = abstractNum?
            .Elements<Level>()
            .FirstOrDefault(candidate => candidate.LevelIndex?.Value == 0)?
            .NumberingFormat?.Val?.Value;

        return format is not null && format != NumberFormatValues.Bullet;
    }

    private sealed class PendingSection(int level, string? title, Provenance provenance)
    {
        public int Level { get; } = level;

        public string? Title { get; } = title;

        public Provenance Provenance { get; } = provenance;

        public List<DocumentNode> Children { get; } = [];
    }
}
