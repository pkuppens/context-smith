using Azure.AI.DocumentIntelligence;
using ContextSmith.Documents.Pdf;

namespace ContextSmith.Documents.Pdf.Tests;

public class DocumentIntelligenceLayoutMapperTests
{
    private static DocumentParagraph Paragraph(string text, ParagraphRole? role, int page, int offset)
        => DocumentIntelligenceModelFactory.DocumentParagraph(
            role: role,
            content: text,
            boundingRegions: [DocumentIntelligenceModelFactory.BoundingRegion(page, [0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f])],
            spans: [DocumentIntelligenceModelFactory.DocumentSpan(offset, text.Length)]);

    private static DocumentTableCell Cell(int row, int column, string text)
        => DocumentIntelligenceModelFactory.DocumentTableCell(rowIndex: row, columnIndex: column, content: text);

    [Fact]
    public void Map_orders_paragraphs_and_tables_by_text_offset_and_skips_table_cell_paragraphs()
    {
        // Document text: "Benefits" at 0-8, table cells at 10-20, "Closing note." at 30-43.
        var result = DocumentIntelligenceModelFactory.AnalyzeResult(
            paragraphs:
            [
                Paragraph("Closing note.", null, 2, 30),
                Paragraph("Benefits", ParagraphRole.SectionHeading, 1, 0),
                Paragraph("Dental", null, 1, 10),
            ],
            tables:
            [
                DocumentIntelligenceModelFactory.DocumentTable(
                    rowCount: 1,
                    columnCount: 2,
                    cells: [Cell(0, 0, "Dental"), Cell(0, 1, "10")],
                    boundingRegions: [DocumentIntelligenceModelFactory.BoundingRegion(1, [0f, 0f, 1f, 0f, 1f, 1f, 0f, 1f])],
                    spans: [DocumentIntelligenceModelFactory.DocumentSpan(10, 10)]),
            ]);

        var layout = DocumentIntelligenceLayoutMapper.Map(result);

        Assert.Collection(
            layout.Elements,
            first => Assert.Equal(new PdfParagraph("Benefits", "sectionHeading", 1), first),
            second =>
            {
                var table = Assert.IsType<PdfTable>(second);
                Assert.Equal(["Dental", "10"], table.Rows[0]);
                Assert.Equal(1, table.PageNumber);
            },
            third => Assert.Equal(new PdfParagraph("Closing note.", null, 2), third));
    }
}
