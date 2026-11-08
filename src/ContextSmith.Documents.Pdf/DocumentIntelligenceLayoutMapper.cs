using Azure.AI.DocumentIntelligence;

namespace ContextSmith.Documents.Pdf;

/// <summary>Converts an Azure Document Intelligence <see cref="AnalyzeResult"/> into a <see cref="PdfLayout"/>.</summary>
public static class DocumentIntelligenceLayoutMapper
{
    /// <summary>
    /// Orders paragraphs and tables by their position in the document text. Paragraphs that
    /// only repeat the text of a table cell are skipped, because the table already holds them.
    /// </summary>
    /// <param name="result">Layout analysis result.</param>
    /// <returns>The layout in reading order.</returns>
    public static PdfLayout Map(AnalyzeResult result)
    {
        var tableSpans = result.Tables
            .SelectMany(table => table.Spans)
            .Select(span => (Start: span.Offset, End: span.Offset + span.Length))
            .ToList();

        var positioned = new List<(int Offset, PdfElement Element)>();

        foreach (var paragraph in result.Paragraphs)
        {
            var offset = paragraph.Spans.Count > 0 ? paragraph.Spans[0].Offset : int.MaxValue;
            if (tableSpans.Any(span => offset >= span.Start && offset < span.End))
            {
                continue;
            }

            var page = paragraph.BoundingRegions.Count > 0 ? paragraph.BoundingRegions[0].PageNumber : 1;
            positioned.Add((offset, new PdfParagraph(paragraph.Content, paragraph.Role?.ToString(), page)));
        }

        foreach (var table in result.Tables)
        {
            var rows = Enumerable.Range(0, table.RowCount)
                .Select(row => (IReadOnlyList<string>)Enumerable.Range(0, table.ColumnCount)
                    .Select(column => table.Cells
                        .FirstOrDefault(cell => cell.RowIndex == row && cell.ColumnIndex == column)?.Content ?? string.Empty)
                    .ToList())
                .ToList();
            var offset = table.Spans.Count > 0 ? table.Spans[0].Offset : int.MaxValue;
            var page = table.BoundingRegions.Count > 0 ? table.BoundingRegions[0].PageNumber : 1;
            positioned.Add((offset, new PdfTable(rows, page)));
        }

        return new PdfLayout(positioned.OrderBy(item => item.Offset).Select(item => item.Element).ToList());
    }
}
