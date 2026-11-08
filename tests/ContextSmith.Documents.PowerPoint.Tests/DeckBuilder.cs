using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace ContextSmith.Documents.PowerPoint.Tests;

/// <summary>One slide to build: optional title, text boxes, an optional table, and optional speaker notes.</summary>
internal sealed record SlideSpec(string? Title = null, string[]? Texts = null, string[][]? Table = null, string? Notes = null);

/// <summary>Builds small in-memory .pptx files, so tests do not depend on committed binaries.</summary>
internal static class DeckBuilder
{
    public static MemoryStream Build(params SlideSpec[] slides)
    {
        var stream = new MemoryStream();
        using (var deck = PresentationDocument.Create(stream, PresentationDocumentType.Presentation))
        {
            var presentationPart = deck.AddPresentationPart();
            presentationPart.Presentation = new Presentation();
            var slideIds = new SlideIdList();
            presentationPart.Presentation.Append(slideIds);
            uint id = 256;

            foreach (var spec in slides)
            {
                var slidePart = presentationPart.AddNewPart<SlidePart>();
                var tree = new ShapeTree(new NonVisualGroupShapeProperties(new NonVisualDrawingProperties { Id = 1, Name = "" }, new NonVisualGroupShapeDrawingProperties(), new ApplicationNonVisualDrawingProperties()), new GroupShapeProperties());
                uint shapeId = 2;

                if (spec.Title is not null)
                {
                    tree.Append(TextShape(shapeId++, spec.Title, new PlaceholderShape { Type = PlaceholderValues.Title }));
                }

                foreach (var text in spec.Texts ?? [])
                {
                    tree.Append(TextShape(shapeId++, text, placeholder: null));
                }

                if (spec.Table is not null)
                {
                    tree.Append(TableFrame(shapeId++, spec.Table));
                }

                slidePart.Slide = new Slide(new CommonSlideData(tree));

                if (spec.Notes is not null)
                {
                    var notesPart = slidePart.AddNewPart<NotesSlidePart>();
                    var notesTree = new ShapeTree(new NonVisualGroupShapeProperties(new NonVisualDrawingProperties { Id = 1, Name = "" }, new NonVisualGroupShapeDrawingProperties(), new ApplicationNonVisualDrawingProperties()), new GroupShapeProperties());
                    notesTree.Append(TextShape(2, string.Empty, new PlaceholderShape { Type = PlaceholderValues.SlideImage }));
                    notesTree.Append(TextShape(3, spec.Notes, new PlaceholderShape { Type = PlaceholderValues.Body, Index = 1 }));
                    notesPart.NotesSlide = new NotesSlide(new CommonSlideData(notesTree));
                }

                slideIds.Append(new SlideId { Id = id++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static P.Shape TextShape(uint id, string text, PlaceholderShape? placeholder)
    {
        var properties = new ApplicationNonVisualDrawingProperties();
        if (placeholder is not null)
        {
            properties.Append(placeholder);
        }

        return new P.Shape(
            new NonVisualShapeProperties(new NonVisualDrawingProperties { Id = id, Name = $"Shape {id}" }, new NonVisualShapeDrawingProperties(), properties),
            new ShapeProperties(),
            new P.TextBody(new A.BodyProperties(), new A.ListStyle(), new A.Paragraph(new A.Run(new A.Text(text)))));
    }

    private static GraphicFrame TableFrame(uint id, string[][] rows)
    {
        var table = new A.Table(new A.TableProperties(), new A.TableGrid());
        foreach (var row in rows)
        {
            var tableRow = new A.TableRow { Height = 370840 };
            foreach (var cell in row)
            {
                tableRow.Append(new A.TableCell(new P.TextBody(new A.BodyProperties(), new A.ListStyle(), new A.Paragraph(new A.Run(new A.Text(cell)))), new A.TableCellProperties()));
            }

            table.Append(tableRow);
        }

        return new GraphicFrame(
            new NonVisualGraphicFrameProperties(new NonVisualDrawingProperties { Id = id, Name = $"Table {id}" }, new NonVisualGraphicFrameDrawingProperties(), new ApplicationNonVisualDrawingProperties()),
            new Transform(),
            new A.Graphic(new A.GraphicData(table) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/table" }));
    }
}
