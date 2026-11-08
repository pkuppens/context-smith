using ContextSmith.Application;
using ContextSmith.Domain;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ContextSmith.Documents.Excel;

/// <summary>Parses Excel (<c>.xlsx</c>) workbooks into the common <see cref="Document"/> tree: one section per sheet, one table per used range.</summary>
public sealed class ExcelDocumentParser : IDocumentParser
{
    /// <inheritdoc/>
    public Task<Document> ParseAsync(DocumentSource source, CancellationToken cancellationToken = default)
    {
        using var workbook = SpreadsheetDocument.Open(source.Content, isEditable: false);
        var workbookPart = workbook.WorkbookPart
            ?? throw new InvalidOperationException($"'{source.SourceId}' has no workbook part.");

        var sharedStrings = workbookPart.SharedStringTablePart?.SharedStringTable
            .Elements<SharedStringItem>().Select(item => item.InnerText).ToList() ?? [];

        var sections = new List<DocumentNode>();
        foreach (var sheet in workbookPart.Workbook.Descendants<Sheet>())
        {
            var name = sheet.Name?.Value ?? "Sheet";
            var worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id!.Value!);
            var provenance = new Provenance(source.SourceId, name);
            var children = new List<DocumentNode> { new Heading { Text = name, Level = 1, Provenance = provenance } };

            var cells = new Dictionary<(int Row, int Column), string>();
            foreach (var cell in worksheetPart.Worksheet.Descendants<Cell>())
            {
                if (cell.CellReference?.Value is not { } reference)
                {
                    continue;
                }

                var text = ReadCell(cell, sharedStrings);
                if (text.Length > 0)
                {
                    cells[ParseReference(reference)] = text;
                }
            }

            if (cells.Count > 0)
            {
                var lastRow = cells.Keys.Max(k => k.Row);
                var lastColumn = cells.Keys.Max(k => k.Column);
                var rows = Enumerable.Range(1, lastRow)
                    .Select(row => (IReadOnlyList<string>)Enumerable.Range(1, lastColumn)
                        .Select(column => cells.GetValueOrDefault((row, column), string.Empty))
                        .ToList())
                    .ToList();
                children.Add(new TableBlock
                {
                    Rows = rows,
                    Provenance = new Provenance(source.SourceId, $"{name}!A1:{ColumnName(lastColumn)}{lastRow}"),
                });
            }

            sections.Add(new Section { Title = name, Provenance = provenance, Children = children });
        }

        return Task.FromResult(new Document
        {
            Metadata = new DocumentMetadata(),
            Provenance = new Provenance(source.SourceId),
            Children = sections,
        });
    }

    // Formula cells carry their cached value in CellValue, so reading CellValue covers them.
    private static string ReadCell(Cell cell, List<string> sharedStrings)
    {
        var raw = cell.CellValue?.Text;
        return cell.DataType?.Value switch
        {
            var type when type == CellValues.SharedString && int.TryParse(raw, out var index) && index < sharedStrings.Count => sharedStrings[index],
            var type when type == CellValues.InlineString => cell.InlineString?.InnerText ?? string.Empty,
            var type when type == CellValues.Boolean => raw == "1" ? "TRUE" : "FALSE",
            _ => raw ?? string.Empty,
        };
    }

    // "AB12" -> (row 12, column 28).
    private static (int Row, int Column) ParseReference(string reference)
    {
        var letters = reference.TakeWhile(char.IsLetter).ToArray();
        var column = letters.Aggregate(0, (acc, ch) => acc * 26 + (char.ToUpperInvariant(ch) - 'A' + 1));
        return (int.Parse(reference[letters.Length..]), column);
    }

    private static string ColumnName(int column)
    {
        var name = string.Empty;
        for (; column > 0; column = (column - 1) / 26)
        {
            name = (char)('A' + (column - 1) % 26) + name;
        }

        return name;
    }
}
