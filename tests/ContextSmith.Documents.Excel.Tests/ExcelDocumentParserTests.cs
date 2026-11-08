using ContextSmith.Application;
using ContextSmith.Domain;

namespace ContextSmith.Documents.Excel.Tests;

public class ExcelDocumentParserTests
{
    private static async Task<Document> ParseAsync(Stream workbook)
        => await new ExcelDocumentParser().ParseAsync(new DocumentSource("plans.xlsx", workbook));

    [Fact]
    public async Task ParseAsync_maps_a_sheet_to_a_section_with_a_heading_and_a_table()
    {
        await using var workbook = WorkbookBuilder.Build(("Benefits", new()
        {
            ["A1"] = "Plan",
            ["B1"] = "Cost",
            ["A2"] = "Dental",
            ["B2"] = 10,
        }));

        var document = await ParseAsync(workbook);

        var section = Assert.IsType<Section>(Assert.Single(document.Children));
        Assert.Equal("Benefits", section.Title);
        Assert.Equal("Benefits", Assert.IsType<Heading>(section.Children[0]).Text);
        var table = Assert.IsType<TableBlock>(section.Children[1]);
        Assert.Equal(["Plan", "Cost"], table.Rows[0]);
        Assert.Equal(["Dental", "10"], table.Rows[1]);
        Assert.Equal("Benefits!A1:B2", table.Provenance.Location);
    }

    [Fact]
    public async Task ParseAsync_reads_shared_strings_and_booleans_as_text()
    {
        await using var workbook = WorkbookBuilder.Build(("Flags", new() { ["A1"] = new SharedText("Eligible"), ["B1"] = true }));

        var table = Assert.Single((await ParseAsync(workbook)).Children.OfType<Section>().Single().Children.OfType<TableBlock>());

        Assert.Equal(["Eligible", "TRUE"], table.Rows[0]);
    }

    [Fact]
    public async Task ParseAsync_pads_gaps_so_every_value_stays_in_its_column()
    {
        // Row 1 starts in column B; row 2 has a value in column D. The used range is A1:D2.
        await using var workbook = WorkbookBuilder.Build(("Gaps", new() { ["B1"] = "x", ["D2"] = "y" }));

        var table = Assert.Single((await ParseAsync(workbook)).Children.OfType<Section>().Single().Children.OfType<TableBlock>());

        Assert.Equal(["", "x", "", ""], table.Rows[0]);
        Assert.Equal(["", "", "", "y"], table.Rows[1]);
        Assert.Equal("Gaps!A1:D2", table.Provenance.Location);
    }

    [Fact]
    public async Task ParseAsync_keeps_an_empty_sheet_as_a_section_without_a_table()
    {
        await using var workbook = WorkbookBuilder.Build(("Empty", []), ("Data", new() { ["A1"] = "v" }));

        var document = await ParseAsync(workbook);

        Assert.Equal(["Empty", "Data"], document.Children.OfType<Section>().Select(s => s.Title));
        Assert.Empty(document.Children.OfType<Section>().First().Children.OfType<TableBlock>());
    }

    [Fact]
    public async Task ParseAsync_reads_the_committed_sample_file()
    {
        await using var stream = File.OpenRead("sample.xlsx");

        var document = await new ExcelDocumentParser().ParseAsync(new DocumentSource("sample.xlsx", stream));

        Assert.Equal(["Benefits", "Leave"], document.Children.OfType<Section>().Select(s => s.Title));
    }
}
