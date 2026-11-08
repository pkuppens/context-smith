using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace ContextSmith.Documents.Excel.Tests;

/// <summary>A cell value stored in the shared string table, as Excel itself writes text.</summary>
internal sealed record SharedText(string Value);

/// <summary>Builds small in-memory .xlsx files, so tests do not depend on committed binaries.</summary>
internal static class WorkbookBuilder
{
    /// <summary>Creates a workbook. Each sheet maps cell references (for example <c>"B2"</c>) to values; numbers are written as numbers, other values as text.</summary>
    public static MemoryStream Build(params (string Name, Dictionary<string, object> Cells)[] sheets)
    {
        var stream = new MemoryStream();
        using (var workbook = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = workbook.AddWorkbookPart();
            workbookPart.Workbook = new Workbook(new Sheets());
            var sheetsElement = workbookPart.Workbook.GetFirstChild<Sheets>()!;
            uint sheetId = 1;
            var sharedStrings = new List<string>();

            foreach (var (name, cells) in sheets)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var data = new SheetData();
                foreach (var rowGroup in cells.GroupBy(c => RowOf(c.Key)).OrderBy(g => g.Key))
                {
                    var row = new Row { RowIndex = (uint)rowGroup.Key };
                    foreach (var cell in rowGroup.OrderBy(c => ColumnOf(c.Key)))
                    {
                        if (cell.Value is SharedText shared)
                        {
                            sharedStrings.Add(shared.Value);
                            row.Append(new Cell { CellReference = cell.Key, DataType = CellValues.SharedString, CellValue = new CellValue((sharedStrings.Count - 1).ToString()) });
                            continue;
                        }

                        if (cell.Value is bool flag)
                        {
                            row.Append(new Cell { CellReference = cell.Key, DataType = CellValues.Boolean, CellValue = new CellValue(flag ? "1" : "0") });
                            continue;
                        }

                        row.Append(cell.Value is int or double
                            ? new Cell { CellReference = cell.Key, CellValue = new CellValue(Convert.ToString(cell.Value, System.Globalization.CultureInfo.InvariantCulture)!) }
                            : new Cell { CellReference = cell.Key, DataType = CellValues.InlineString, InlineString = new InlineString(new Text((string)cell.Value)) });
                    }

                    data.Append(row);
                }

                worksheetPart.Worksheet = new Worksheet(data);
                sheetsElement.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId++, Name = name });
            }

            if (sharedStrings.Count > 0)
            {
                var stringsPart = workbookPart.AddNewPart<SharedStringTablePart>();
                stringsPart.SharedStringTable = new SharedStringTable(sharedStrings.Select(text => (OpenXmlElement)new SharedStringItem(new Text(text))));
            }
        }

        stream.Position = 0;
        return stream;
    }

    private static int RowOf(string reference) => int.Parse(new string(reference.Where(char.IsDigit).ToArray()));

    private static int ColumnOf(string reference)
        => reference.TakeWhile(char.IsLetter).Aggregate(0, (acc, ch) => acc * 26 + (ch - 'A' + 1));
}
