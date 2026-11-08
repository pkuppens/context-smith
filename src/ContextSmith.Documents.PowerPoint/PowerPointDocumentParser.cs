using ContextSmith.Application;
using ContextSmith.Domain;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using DomainParagraph = ContextSmith.Domain.Paragraph;

namespace ContextSmith.Documents.PowerPoint;

/// <summary>Parses PowerPoint (<c>.pptx</c>) files into the common <see cref="Document"/> tree: one section per slide.</summary>
public sealed class PowerPointDocumentParser : IDocumentParser
{
    /// <inheritdoc/>
    public Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        using var deck = PresentationDocument.Open(source.Content, isEditable: false);
        var presentationPart = deck.PresentationPart
            ?? throw new InvalidOperationException($"'{source.SourceId}' has no presentation part.");

        var sections = new List<DocumentNode>();
        var slideNumber = 0;
        foreach (var slideId in presentationPart.Presentation.SlideIdList?.Elements<SlideId>() ?? [])
        {
            slideNumber++;
            var slidePart = (SlidePart)presentationPart.GetPartById(slideId.RelationshipId!.Value!);
            var provenance = new Provenance(source.SourceId, $"slide-{slideNumber}");
            var children = new List<DocumentNode>();
            string? title = null;

            foreach (var element in slidePart.Slide.CommonSlideData!.ShapeTree!.Elements())
            {
                if (element is GraphicFrame frame)
                {
                    var table = frame.Descendants<A.Table>().FirstOrDefault();
                    if (table is not null)
                    {
                        var rows = table.Elements<A.TableRow>()
                            .Select(row => (IReadOnlyList<string>)row.Elements<A.TableCell>().Select(cell => cell.InnerText.Trim()).ToList())
                            .ToList();
                        children.Add(new TableBlock { Rows = rows, Provenance = provenance });
                    }

                    continue;
                }

                if (element is not Shape shape)
                {
                    continue;
                }

                var text = string.Join(' ', shape.TextBody?.Elements<A.Paragraph>().Select(p => p.InnerText.Trim()).Where(t => t.Length > 0) ?? []);
                if (text.Length == 0)
                {
                    continue;
                }

                var placeholderType = shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.GetFirstChild<PlaceholderShape>()?.Type?.Value;
                if (placeholderType == PlaceholderValues.Title || placeholderType == PlaceholderValues.CenteredTitle)
                {
                    title = text;
                    children.Insert(0, new Heading { Text = text, Level = 1, Provenance = provenance });
                }
                else
                {
                    children.Add(new DomainParagraph { Text = text, Provenance = provenance });
                }
            }

            // Speaker notes stay with the slide they explain. The notes slide also holds a slide-image
            // and a slide-number placeholder; only the body placeholder carries the note text.
            var notesText = slidePart.NotesSlidePart?.NotesSlide.CommonSlideData?.ShapeTree?.Elements<Shape>()
                .Where(shape => shape.NonVisualShapeProperties?.ApplicationNonVisualDrawingProperties?.GetFirstChild<PlaceholderShape>()?.Type?.Value == PlaceholderValues.Body)
                .Select(shape => shape.TextBody?.InnerText.Trim() ?? string.Empty)
                .FirstOrDefault(text => text.Length > 0);
            if (notesText is not null)
            {
                children.Add(new DomainParagraph { Text = notesText, Provenance = new Provenance(source.SourceId, $"slide-{slideNumber}/notes") });
            }

            if (title is null)
            {
                title = $"Slide {slideNumber}";
                children.Insert(0, new Heading { Text = title, Level = 1, Provenance = provenance });
            }

            sections.Add(new Section { Title = title, Provenance = provenance, Children = children });
        }

        return Task.FromResult(new Document
        {
            Metadata = new DocumentMetadata(),
            Provenance = new Provenance(source.SourceId),
            Children = sections,
        });
    }
}
